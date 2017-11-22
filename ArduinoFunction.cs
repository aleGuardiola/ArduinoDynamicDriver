using System;
using System.Collections.Generic;
using System.Text;

namespace ArduinoDynamicDriver
{
    public class ArduinoFunction
    {
        public readonly byte FunctionId;
        public readonly IList<SupportedType> ArgumentTypes;
        public readonly SupportedType ReturnType;
        object[] Arguments;

        public ArduinoFunction(byte functionId, IList<SupportedType> argumentTypes, SupportedType returnType, params object[] arguments)
        {
            FunctionId = functionId;
            ArgumentTypes = argumentTypes ?? throw new ArgumentNullException(nameof(argumentTypes));
            ReturnType = returnType;
            Arguments = arguments;
        }

        public IList<Byte> GetDataBytes()
        {
            var result = new List<byte>();
            for (int i = 0; i < Arguments.Length; i++)
            {
                var argument = Arguments[i];
                var argumentType = ArgumentTypes[i];

                switch (argumentType)
                {
                    case SupportedType.BOOLEANT:
                        var boolean = (bool)argument;
                        result.Add(boolean ? (byte)1 : (byte)0);
                        break;
                    case SupportedType.CHART:
                        result.Add(BitConverter.GetBytes(Convert.ToSByte(argument))[0]); 
                        break;
                    case SupportedType.UCHART:
                        result.Add(Convert.ToByte(argument));
                        break;
                    case SupportedType.BYTET:
                        result.Add(Convert.ToByte(argument));
                        break;
                    case SupportedType.INTT:
                        var integer = Convert.ToInt16(argument);
                        var integerReperesentation = BitConverter.GetBytes(integer);
                        result.AddRange(integerReperesentation);
                        break;
                    case SupportedType.UINTT:
                        var uinteger = Convert.ToUInt16(argument);
                        var uintegerReperesentation = BitConverter.GetBytes(uinteger);
                        result.AddRange(uintegerReperesentation);
                        break;
                    case SupportedType.LONG:
                        var lon = Convert.ToInt32(argument);
                        var lonRepresentation = BitConverter.GetBytes(lon);
                        result.AddRange(lonRepresentation);
                        break;
                    case SupportedType.ULONG:
                        var ulon = Convert.ToUInt32(argument);
                        var ulonRepresentation = BitConverter.GetBytes(ulon);
                        result.AddRange(ulonRepresentation);
                        break;
                    case SupportedType.SHORT:
                        var shor = Convert.ToInt16(argument);
                        var shorReprestation = BitConverter.GetBytes(shor);
                        result.AddRange(shorReprestation);
                        break;                    
                    case SupportedType.VOIDT:
                        throw new ArgumentException("An argument cant be VOIDT");                        
                }
            }

            return result;
        }
    }
}