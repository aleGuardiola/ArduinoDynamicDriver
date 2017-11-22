using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ArduinoDynamicDriver.SerialProtocol;

namespace ArduinoDynamicDriver
{
    internal class ArduinoDriverSerialPort
    {
        private const int MaxSendRetries = 6;
        private const int MaxSyncRetries = 3;
        ISerialStream serialStream;

        internal ArduinoDriverSerialPort(ISerialStream serialStream)
        {
            this.serialStream = serialStream;
        }

        public void Open()
        {
            serialStream.Open();
        }

        public void Close()
        {
            serialStream.Close();
        }

        public void Dispose()
        {
            serialStream.Dispose();
        }

        private bool GetSync()
        {
            try
            {
                var bytes = CommandConstants.SyncBytes;
                serialStream.Write(bytes, 0, bytes.Length);
                var responseBytes = ReadCurrentReceiveBuffer(4);
                return responseBytes.Length == 4
                       && responseBytes[0] == bytes[3]
                       && responseBytes[1] == bytes[2]
                       && responseBytes[2] == bytes[1]
                       && responseBytes[3] == bytes[0];
            }
            catch (TimeoutException e)
            {
                //logger.Trace("Timeout while trying to get sync...");
                return false;
            }
            catch (Exception e)
            {
                //logger.Trace("Exception while trying to get sync ('{0}')", e.Message);
                return false;
            }
        }

        private bool ExecuteCommandHandShake(byte command, byte length)
        {
            var bytes = new byte[] { 0xfb, command, length };
            serialStream.Write(bytes, 0, bytes.Length);
            var responseBytes = ReadCurrentReceiveBuffer(3);
            return responseBytes.Length == 3
                   && responseBytes[0] == bytes[2]
                   && responseBytes[1] == bytes[1]
                   && responseBytes[2] == bytes[0];
        }

        internal byte[] Send(ArduinoFunction request, int maxSendRetries = MaxSendRetries)
        {
            var sendRetries = 0;
            while (sendRetries++ < maxSendRetries)
            {
                try
                {
                    // First try to get sync (send FF FE FD FC and require FC FD FE FF as a response).
                    bool hasSync;
                    var syncRetries = 0;
                    while (!(hasSync = GetSync()) && syncRetries++ < MaxSyncRetries)
                    {

                    }
                    if (!hasSync)
                    {
                        var errorMessage = string.Format("Unable to get sync after {0} tries!", MaxSyncRetries);
                        throw new IOException(errorMessage);
                    }

                    // Now send the command handshake (send FB as start of message marker + the command byte + length byte).
                    // Expect the inverse (length byte followed by command byte followed by FB) as a command ACK.
                    var requestBytes = request.GetDataBytes().ToArray();
                    var requestBytesLength = requestBytes.Length;

                    if (!ExecuteCommandHandShake(request.FunctionId, (byte)requestBytesLength))
                    {
                        var errorMessage = string.Format("Unable to configure command handshake for command {0}.", request);
                        //logger.Warn(errorMessage);
                        throw new IOException(errorMessage);
                    }

                    // Write out all packet bytes, followed by a Fletcher 16 checksum!
                    // Packet bytes consist of:
                    // 1. Command byte repeated
                    // 2. Request length repeated
                    // 3. The actual request bytes
                    // 4. Two fletcher-16 checksum bytes calculated over (1 + 2 + 3)
                    var packetBytes = new byte[requestBytesLength + 4];
                    packetBytes[0] = request.FunctionId;
                    packetBytes[1] = (byte)requestBytesLength;
                    Buffer.BlockCopy(requestBytes, 0, packetBytes, 2, requestBytesLength);
                    var fletcher16CheckSum = CalculateFletcher16Checksum(packetBytes, requestBytesLength + 2);
                    var f0 = (byte)(fletcher16CheckSum & 0xff);
                    var f1 = (byte)((fletcher16CheckSum >> 8) & 0xff);
                    var c0 = (byte)(0xff - (f0 + f1) % 0xff);
                    var c1 = (byte)(0xff - (f0 + c0) % 0xff);
                    packetBytes[requestBytesLength + 2] = c0;
                    packetBytes[requestBytesLength + 3] = c1;

                    serialStream.Write(packetBytes, 0, requestBytesLength + 4);

                    // Write out all bytes written marker (FA)
                    serialStream.Write(new[] { CommandConstants.AllBytesWritten }, 0, 1);

                    // Expect response message to drop to be received in the following form:
                    // F9 (start of response marker) followed by response length
                    var responseBytes = ReadCurrentReceiveBuffer(2);
                    var startOfResponseMarker = responseBytes[0];
                    var responseLength = responseBytes[1];
                    if (startOfResponseMarker != CommandConstants.StartOfResponseMarker)
                    {
                        var errorMessage = string.Format("Did not receive start of response marker but {0}!", startOfResponseMarker);
                        throw new IOException(errorMessage);
                    }
                    
                    // Read x responsebytes
                    if(responseLength == 0xfc)                    
                        return null;
                    
                    responseBytes = ReadCurrentReceiveBuffer(responseLength);
                    return responseBytes;
                }
                catch (TimeoutException ex)
                {
                    // logger.Debug(ex, "TimeoutException in Send occurred, retrying ({0}/{1})!", sendRetries, MaxSendRetries);
                }
                catch (Exception ex)
                {
                    // logger.Debug("Exception: '{0}'", ex.Message);
                }
            }
            return null;
        }

        private static ushort CalculateFletcher16Checksum(IReadOnlyList<byte> checkSumBytes, int count)
        {
            ushort sum1 = 0;
            ushort sum2 = 0;
            for (var index = 0; index < count; ++index)
            {
                sum1 = (ushort)((sum1 + checkSumBytes[index]) % 255);
                sum2 = (ushort)((sum2 + sum1) % 255);
            }
            return (ushort)((sum2 << 8) | sum1);
        }

        private byte[] ReadCurrentReceiveBuffer(int numberOfBytes)
        {
            var result = new byte[numberOfBytes];
            var retrieved = 0;
            var retryCount = 0;

            try
            {
                while (retrieved < numberOfBytes && retryCount++ < 4)
                    retrieved += serialStream.Read(result, retrieved, numberOfBytes - retrieved);
            }
            catch (Exception e)
            {

            }
            if (retrieved < numberOfBytes)
            {
                //logger.Info("Ended up reading short (expected {0} bytes, got only {1})...",
                //    numberOfBytes, retrieved);
            }
            return result;
        }
    }
}