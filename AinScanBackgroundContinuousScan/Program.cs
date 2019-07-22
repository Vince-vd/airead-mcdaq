using System;
using MccDaq;
using System.IO;
using System.Text;

namespace AinScanBackgroundContinuousScan
{
    class Program
    {
        public const int BLOCKSIZE      = 5000;
        public const int CHANCOUNT      = 4;
        public const int FIRSTCHANNEL   = 0;
        public const int LASTCHANNEL    = 3;
        public const int FREQ           = 5000;
        public const int BUFFERSIZE     = BLOCKSIZE * CHANCOUNT;
        public const int HALFBUFFSIZE   = BUFFERSIZE / 2;
        public const string DEVICE = "1608G";

        public static StreamWriter fStream;
        static void Main(string[] args)
        {
            System.ConsoleKeyInfo cki = new System.ConsoleKeyInfo();
            MccDaq.ErrorInfo RetVal;

            int     BoardNum        = 0;
            int     DeviceChannels  = 0;
            int     Rate            = FREQ;
            bool    ReadLower       = true;

            BoardNum = GetBoardNum(DEVICE);

            if( BoardNum == -1 )
            {
                Console.WriteLine("No USB-{0} detected!", DEVICE);
                cki = Console.ReadKey();
            }
            else
            {
                MccBoard daq = new MccDaq.MccBoard( BoardNum );
                
                daq.BoardConfig.GetNumAdChans(out DeviceChannels);
                
                if (DeviceChannels > 8)
                    Console.WriteLine( "Single-Ended Channels" );
                else
                    Console.WriteLine( "Differentially-Ended Channels" );


                IntPtr buffer = MccService.ScaledWinBufAllocEx( BUFFERSIZE );

                if( buffer == IntPtr.Zero )
                {
                    Console.WriteLine( "Bad Handle" );
                    return;
                }

                short[] chArray = new short[CHANCOUNT]; //configuration array for channel numbers
                Range[] chRange = new Range[CHANCOUNT]; //configuration array for input ranges

                chArray[0] = 0;
                chArray[1] = 1;
                chArray[2] = 2;
                chArray[3] = 3;

                chRange[0] = Range.Bip10Volts;
                chRange[1] = Range.Bip10Volts;
                chRange[2] = Range.Bip10Volts;
                chRange[3] = Range.Bip10Volts;

                RetVal = daq.ALoadQueue( chArray, chRange, CHANCOUNT );
                IsError(RetVal);

                //setup the acquisiton
                RetVal = daq.AInScan(   FIRSTCHANNEL, 
                                        LASTCHANNEL, 
                                        BUFFERSIZE, 
                                        ref Rate, 
                                        Range.Bip10Volts, 
                                        buffer,
                                        ScanOptions.Background | ScanOptions.ScaleData | ScanOptions.Continuous
                                    );
                IsError(RetVal);

                fStream = new StreamWriter(@"C:\Users\Public\Documents\DataFile1608G.asc");
                CreateFileHeaders(chArray); //writes basic info to the beginning of the file

                int Count = 0;
                int Index = 0;
                short daqStatus;

                double[] theArray = new double[BUFFERSIZE];

                

                //Loop until key press
                do{
                    RetVal = daq.GetStatus( out daqStatus, out Count, out Index, FunctionType.AiFunction );
                    if ((Index >= HALFBUFFSIZE) & ReadLower) //check for 50% more data
                    {
                        //get lower half of buffer - ScaledWinBufToArray returns engineering units
                        RetVal = MccService.ScaledWinBufToArray(buffer, theArray, 0, HALFBUFFSIZE);
                        IsError(RetVal);

                        DisplayData(theArray, HALFBUFFSIZE/CHANCOUNT);
                        ReadLower = false; //flag that controls the next read
                    }
                    else if ((Index < HALFBUFFSIZE) & !ReadLower)
                    {
                        //get the upper half  - ScaledWinBufToArray returns engineering units
                        RetVal = MccService.ScaledWinBufToArray(buffer, theArray, HALFBUFFSIZE, HALFBUFFSIZE);
                        IsError(RetVal);

                        DisplayData(theArray, HALFBUFFSIZE/CHANCOUNT);
                        ReadLower = true;//flag that controls the next read
                    }
                   

                } while (!Console.KeyAvailable);

                cki = Console.ReadKey();

                //flush any buffered data out to disk
                fStream.Close();

                //stop the  acquisition
                RetVal = daq.StopBackground(FunctionType.AiFunction);

                //free up memory
                MccService.WinBufFreeEx(buffer);

                WaitForKey();
            }
        }
        public static int IsError(ErrorInfo e)
        {
            if (e.Value != 0)
            {
                Console.WriteLine(e.Message);
                return 1;
            }
            return 0;
        }
        public static int GetBoardNum(string dev)
        {
            for (int BoardNum = 0; BoardNum < 99; BoardNum++)
            {
                MccDaq.MccBoard daq = new MccDaq.MccBoard(BoardNum);
                if (daq.BoardName.Contains(dev))
                {
                    Console.WriteLine("USB-{0} board number = {1}", dev, BoardNum.ToString());
                    daq.FlashLED();
                    return BoardNum;
                }
            }
            return -1;
        }
        public static void WaitForKey()
        {
            Console.WriteLine("\nPress <SpaceBar> to continue...");

            System.ConsoleKeyInfo cki;
            do
            {
                cki = Console.ReadKey();
            } while (cki.Key != ConsoleKey.Spacebar);
        }
        public static void DisplayData(double[] datArray, int rows)
        {
            StringBuilder output = new StringBuilder();
            //Writes data to screen and to file
            int i = 0;
            for (int row = 0; row < rows; row++)
            {
                output.Append(row);
                for (int c = 0; c < CHANCOUNT; c++)
                {
                    output.Append(datArray[i].ToString("0.0000").PadLeft(10));
                    output.Append("\t");
                    i++;
                }
                output.Append("\r\n");
            }
            Console.Write(output);
            fStream.Write(output);
            fStream.Flush();
        }
        public static void CreateFileHeaders(short[] x)
        {
            Console.WriteLine("This program reads channels {0} through {1}\n", FIRSTCHANNEL, LASTCHANNEL);

            //''''''''''''''''''''' create text file header strings ''''''''''''''''
            string[] hdr = new string[5];
            hdr[0] = "Recording date     : {0}";
            hdr[1] = "Block length       : {0}";
            hdr[2] = "Delta              : {0} sec.";
            hdr[3] = "Number of channels : {0}";

            //''''''''''''''''''''''' build column headers string '''''''''''''''''''

            hdr[4] = hdr[4] + "Row\t";
            for (int j = 0; j < CHANCOUNT; j++)
            {
                hdr[4] = hdr[4] + String.Format("In{0} [V]", x[j]);
                hdr[4] = hdr[4] + "\t";
               
            }
            hdr[4].Trim('\t');

            //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            DateTime MyDate = DateTime.Now;

            fStream.WriteLine(hdr[0], MyDate);
            fStream.WriteLine(hdr[1], HALFBUFFSIZE / CHANCOUNT);
            fStream.WriteLine(hdr[2], (1 / FREQ));
            fStream.WriteLine(hdr[3], CHANCOUNT);
            fStream.WriteLine(hdr[4]);

            Console.WriteLine(hdr[0], MyDate);
            Console.WriteLine(hdr[1], HALFBUFFSIZE / CHANCOUNT);
            Console.WriteLine(hdr[2], (1 / FREQ));
            Console.WriteLine(hdr[3], CHANCOUNT);
            Console.WriteLine(hdr[4]);

        }

    }
}
