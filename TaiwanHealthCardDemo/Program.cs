using System;
using System.Linq;
using System.Text;
using PCSC;
using PCSC.Iso7816;

namespace TaiwanHealthCardDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.Unicode;

            using (var ctx = ContextFactory.Instance.Establish(SCardScope.User))
            {
                var firstReader = ctx
                    .GetReaders()
                    .FirstOrDefault();

                if (firstReader == null)
                {
                    Console.WriteLine("No reader connected.");
                    return;
                }

                using (var isoReader = new IsoReader(context: ctx, readerName: firstReader, mode: SCardShareMode.Shared, protocol: SCardProtocol.Any))
                {
                    var selectApdu = new CommandApdu(IsoCase.Case4Short, isoReader.ActiveProtocol)
                    {
                        CLA = 0x00,
                        INS = 0xA4,
                        P1 = 0x04,
                        P2 = 0x00,
                        Data = new byte[] { 0xD1, 0x58, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x11 },
                        Le = 0x00
                    };

                    Console.WriteLine("Send Select APDU command: \r\n{0}", BitConverter.ToString(selectApdu.ToArray()));

                    var selectResponse = isoReader.Transmit(selectApdu);
                    Console.WriteLine("SW1 SW2 = {0:X2} {1:X2}", selectResponse.SW1, selectResponse.SW2);

                    var readProfileApdu = new CommandApdu(IsoCase.Case4Short, isoReader.ActiveProtocol)
                    {
                        CLA = 0x00,
                        INS = 0xCA,
                        P1 = 0x11,
                        P2 = 0x00,
                        Data = new byte[] { 0x00, 0x00 },
                        Le = 0x00
                    };

                    Console.WriteLine("Send Read Profile APDU command: \r\n{0}", BitConverter.ToString(readProfileApdu.ToArray()));

                    var profileResponse = isoReader.Transmit(readProfileApdu);
                    Console.WriteLine("SW1 SW2 = {0:X2} {1:X2}", profileResponse.SW1, profileResponse.SW2);

                    if (profileResponse.HasData)
                    {
                        var data = profileResponse.GetData();
                        var cardNumber = GetCardNumber(data[..12]);
                        var cardHolderName = GetHolderName(data[12..32]);
                        var holderIdNum = GetHolderIdNum(data[32..42]);
                        var holderBirth = GetHolderBirthStr(data[43..49]);
                        var holderSex = GetGender(data[49..50]);
                        var cardIssueDate = GetIssueDate(data[51..57]);

                        Console.WriteLine("\r\n=== Health Card Content: ===\r\n");
                        Console.WriteLine(
                            $"Card Number  : { cardNumber }\r\n" +
                            $"Holder Name  : { cardHolderName }\r\n" +
                            $"Holder ID    : { holderIdNum }\r\n" +
                            $"Holder Sex   : { holderSex }\r\n" +
                            $"Holder Birth : { holderBirth }\r\n" +
                            $"Card Issue On: { cardIssueDate }");
                    }
                }
            }
        }

        private static string GetIssueDate(byte[] input)
        {
            var asciiEncoding = Encoding.ASCII;
            return asciiEncoding.GetString(input);
        }

        private static string GetGender(byte[] input)
        {
            var asciiEncoding = Encoding.ASCII;
            return asciiEncoding.GetString(input);
        }

        private static string GetHolderBirthStr(byte[] input)
        {
            var asciiEncoding = Encoding.ASCII;
            return asciiEncoding.GetString(input);
        }

        private static string GetHolderIdNum(byte[] input)
        {
            var asciiEncoding = Encoding.ASCII;
            return asciiEncoding.GetString(input);
        }

        private static string GetCardNumber(byte[] input)
        {
            var asciiEncoding = Encoding.ASCII;
            return asciiEncoding.GetString(input);
        }

        private static string GetHolderName(byte[] input)
        {
            string holderName;

            var big5EncodingInfo = Encoding.GetEncodings().FirstOrDefault(_ => _.Name == "big5");

            if (big5EncodingInfo == null)
            {
                // Register a Big5 coding provider
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                var encoding = Encoding.GetEncoding("big5");

                holderName = encoding.GetString(input);
            }
            else
            {
                holderName = big5EncodingInfo.GetEncoding().GetString(input);
            }

            return holderName;
        }
    }
}
