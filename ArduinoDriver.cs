using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using ArduinoDynamicDriver.SerialProtocol;
using ArduinoDynamicDriver;

namespace ArduinoDynamicDriver
{
    public class ArduinoDriver //: IDisposable
    {
        private const int CurrentProtocolMajorVersion = 1;
        private const int CurrentProtocolMinorVersion = 2;
        private const int DriverBaudRate = 115200;
        private ArduinoDriverSerialPort port;
        
        /// <summary>
        /// Creates a new ArduinoDriver instance. The relevant portname will be autodetected if possible.
        /// </summary>
        /// <param name="arduinoModel"></param>
        /// <param name="autoBootstrap"></param>
        public ArduinoDriver(ISerialStream serialPortStream)
        {
            serialPortStream.SetBaudRate(DriverBaudRate);
            serialPortStream.Open();
            port = new ArduinoDriverSerialPort(serialPortStream);            
        }
        
        public object Execute(ArduinoFunction function)
        {
            var result = port.Send(function);

            object netObject=null;
            switch (function.ReturnType)
            {
                case SupportedType.BOOLEANT:
                    netObject = result[0] == 0 ? false : true;
                    break;
                case SupportedType.CHART:
                    netObject = (sbyte)result[0];
                    break;
                case SupportedType.UCHART:
                    netObject = result[0];
                    break;
                case SupportedType.BYTET:
                    netObject = result[0];
                    break;
                case SupportedType.INTT:
                    netObject = BitConverter.ToInt16(result, 0);
                    break;
                case SupportedType.UINTT:
                    netObject = BitConverter.ToUInt16(result, 0);
                    break;
                case SupportedType.LONG:
                    netObject = BitConverter.ToInt32(result, 0);
                    break;
                case SupportedType.ULONG:
                    netObject = BitConverter.ToUInt32(result, 0);
                    break;
                case SupportedType.SHORT:
                    netObject = BitConverter.ToInt16(result, 0);                    
                    break;                                
            }

            return netObject;

        }
    }
}
