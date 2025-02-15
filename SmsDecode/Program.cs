using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmsDecode
{
    class Program
    {
        static void Main(string[] args)
        {

            string smsFile      = "..\\..\\..\\sms.vmsg";
            string smsWriteFile = "..\\..\\..\\smsDecoded.txt";
            string emojiList    = "..\\..\\..\\emojiList.txt";
            string emojiString  = "..\\..\\..\\smsMessageString.txt";

            string matchPhoneNumber = "+441234567890";

            int lineNumber = 0;
            int messagesTotal = 0;
            int messagesMatched = 0;
            int messagesFrom = 0;
            int messagesTo = 0;
            string msgFrom = "";
            string msgText = "";
            string readLine = "";
            string msgDate = "";
            bool skipMsg = false;
            string[] messages = new string[3000];

            //Check file exists
            if (!System.IO.File.Exists(smsFile))
            { 
                Console.WriteLine($"No file exists {smsFile}");
                return;
            }

            //Read file and create output files
            System.IO.StreamReader streamReader = new System.IO.StreamReader(smsFile);
            System.IO.StreamWriter streamWriter = new System.IO.StreamWriter(smsWriteFile);
            System.IO.StreamWriter emojiWriter = new System.IO.StreamWriter(emojiList);
            System.IO.StreamWriter emojiStringWriter = new System.IO.StreamWriter(emojiString);

            while (!streamReader.EndOfStream)
            {
                readLine = streamReader.ReadLine();
                ++lineNumber;
                //Begining of new message, initialize variables
                if (readLine == "BEGIN:VMSG")
                {
                    ++messagesTotal;
                    msgDate = msgFrom = msgText= "";
                    skipMsg = false;
                }
                //continue to read the next if skipping this message 
                else if (skipMsg)
                {
                    continue;
                }
                //Filter messages to/from specific phone number
                else if (readLine.Substring(0, 4) == "TEL:")
                {
                    if (readLine == $"TEL:{matchPhoneNumber}")
                    {
                        ++messagesMatched;
                    }
                    else
                    {
                        skipMsg = true;
                    }
                }
                //Split to and from messages
                else if (readLine.Substring(0, 6) == "X-BOX:")
                {
                    if (readLine == "X-BOX:SENDBOX" || readLine == "X-BOX:OUTBOX")
                    {
                        ++messagesTo;
                        msgFrom = "©️";
                    }
                    else if (readLine == "X-BOX:INBOX")
                    {
                        ++messagesFrom;
                        msgFrom = "®️";
                    }
                    else
                    {
                        Console.WriteLine("err01 " + lineNumber + readLine);
                        break;
                    }
                }
                // Store date and time
                else if (readLine.Substring(0, 5) == "Date:")
                {
                    msgDate = readLine.Substring(5, 19);
                }
                //Get message lines
                else if (readLine.Substring(0, 8) == "Subject;")
                {
                    while (readLine[readLine.Length - 1] == '=')
                    {
                        if (msgText == "")
                        {
                            msgText = readLine.Substring(48, readLine.Length - 49);
                        }
                        else
                        {
                            msgText += readLine.Substring(0, readLine.Length - 1);
                        }
                        readLine = streamReader.ReadLine();
                        ++lineNumber;
                    }
                    if (msgText == "")
                    {
                        msgText = readLine.Substring(48);
                    }
                    else
                    {
                        msgText += readLine;
                    }
                    if (msgFrom == "" || msgDate == "" || msgText == "")
                    {
                        Console.WriteLine("err02 " + lineNumber + readLine);
                        break;
                    }

                    //Process message string
                    int hexCharIndex = 0;
                    int startIndex = 0;
                    string hexString = "";

                    //Decode Emojis
                    while ((hexCharIndex = msgText.IndexOf("=F0=9F")) != -1)
                    {
                        hexString = msgText.Substring(hexCharIndex, 12);
                        if (hexString[6] == '=' || hexString[9] == '=')
                        {
                            msgText = msgText.Replace((hexString + " "), HexDecode(hexString));
                            msgText = msgText.Replace(hexString, HexDecode(hexString));                           
                        }
                        else
                        {
                            Console.WriteLine("err03 " + lineNumber + msgText);
                            break;
                        }
                    }
                    
                    //Decode shorter hex symbols
                    while (startIndex < msgText.Length &&(hexCharIndex = msgText.IndexOf("=", startIndex)) != -1)
                    {
                        startIndex = hexCharIndex + 1;
                        if (msgText.Length - hexCharIndex >= 9 && msgText[hexCharIndex + 3] == '=' && msgText[hexCharIndex + 6] == '=')
                        {
                            hexString = msgText.Substring(hexCharIndex, 9);
                            msgText = msgText.Replace((hexString + " "), HexDecode(hexString));
                            msgText = msgText.Replace(hexString, HexDecode(hexString));
                        }
                        else if (msgText.Length - hexCharIndex >= 6 && msgText[hexCharIndex + 3] == '=')
                        { 
                            hexString = msgText.Substring(hexCharIndex, 6);
                            msgText = msgText.Replace(hexString, HexDecode(hexString));
                        }
                        else if (msgText.Length - hexCharIndex >= 3)
                        {
                            hexString = msgText.Substring(hexCharIndex, 3);
                            //Process '=' character 
                            if (hexString == "=3D")
                            {
                                msgText = msgText.Remove(hexCharIndex, 3).Insert(hexCharIndex, "=");
                                continue;
                            }
                            else
                            {
                                msgText = msgText.Replace(hexString, HexDecode(hexString));
                                if (hexString == HexDecode(hexString)) 
                                { 
                                    Console.WriteLine("err04 " + lineNumber + msgText); 
                                    break; 
                                }
                            }
                        }
                        else {Console.WriteLine(lineNumber + msgText);}

                    }
                    //Add a space to the end of each message, and remove any double spaces.
                    msgText = (msgText + " ").Replace("  ", " ");
                    //Output to console and to the list of messages
                    streamWriter.WriteLine(messagesMatched.ToString().PadLeft(4, '0') + $" | {msgDate} | {msgFrom} | {msgText}");
                    messages[messagesMatched - 1] = $"{msgFrom}: {msgText}";

                    skipMsg = true;
                }
            }
            streamReader.Close();
            streamWriter.Close();

            //write all decoded symbols and emojis, to check they output correctly
            foreach (string emoji in emojiTable)
            {
                emojiWriter.Write(emoji);
            }
            emojiWriter.Close();

            //write all messages to a single string in reverse order
            for (int i = 0; i < messages.Length; i++)
            {
                if (messages[messages.Length - i - 1] != null)
                    emojiStringWriter.Write(messages[messages.Length - i - 1]);
            }
            emojiStringWriter.Close();

            Console.WriteLine($"Lines processed: {lineNumber}");
            Console.WriteLine($"Messages processed: {messagesTotal}");
            Console.WriteLine($"Messages matched: {messagesMatched}");
            Console.WriteLine($"Messages from number: {messagesFrom}");
            Console.WriteLine($"Messages to number: {messagesTo}");
            Console.Read();
        }

        static string[] emojiTable = new string[130];

        // VMSG files encode symbols and emojis in UTF8 format
        // For example '=F0=9F=8E=81' is 🎁
        // Decode this format into the actual characters
        static private string HexDecode (string hexString)
        {
            int hexLength = hexString.Length;
            int numBytes = hexLength / 3;
            string [] hexArray = new string[numBytes];
            byte[] hexByte = new byte[numBytes];
            int byteCounter = 0;

            //reject certain hex values: '🏻' and line feeds
            string[] rejectList = { "=F0=9F=8F=BB", "=0A", "=0A=0A", "=0A=0A=0A"};
            if (rejectList.Contains(hexString))
            {
                return "";
            }

            for (int i = 0; i < hexLength; ++i)
            {
                if (hexString[i] == '=' )
                {
                    continue;
                }
                hexArray[byteCounter] += hexString[i].ToString();
                if (hexArray[byteCounter].Length == 2)
                {
                    hexByte[byteCounter] = Byte.Parse(hexArray[byteCounter], System.Globalization.NumberStyles.AllowHexSpecifier);
                    ++byteCounter;
                }
            }

            //add characters to table for later to check if they print
            for (int i = 0; i < emojiTable.Length; i++)
            {
                if (Encoding.UTF8.GetString(hexByte) == emojiTable[i])
                {
                    break;
                }
                if (emojiTable[i] == null)
                {
                    emojiTable[i] = Encoding.UTF8.GetString(hexByte);
                    break;
                }
            }

            return Encoding.UTF8.GetString(hexByte);
        }
    }
}

