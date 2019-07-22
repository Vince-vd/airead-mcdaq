using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace AinScanBackgroundContinuousScan
{
    
    class test_write
    {
        public static StreamWriter fStream;
        static void not_main(string[] args)
        {
            fStream = new StreamWriter(@"C:\Users\Public\Documents\DataFile1608G.asc");
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            StringBuilder output = new StringBuilder();
            for (int row = 0; row < 10000; row++)
            {
                for (int c = 0; c < 3; c++)
                {
                    output.Append(c.ToString("0.0000").PadLeft(10));
                    output.Append("\t");
                    //Console.Write("{0}\t", c.ToString("0.0000").PadLeft(10));
                    //fStream.Write("{0}\t", c.ToString("0.0000"));
                }
                output.Append("\r\n");
                if ((row % 3000) == 0)
                {
                    Console.Write(output);
                    output.Length = 0;
                }
                //Console.Write("\r\n");
                //fStream.Write("\r\n");
            }
            Console.Write(output);
            //Console.Write(output);
            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            Console.Write("Wrote 10000 rows in {0} seconds, that's {1} rows per second", ts.Seconds, 10000 / ts.Seconds);
            Console.ReadLine();
            fStream.Flush();
        }
    }
}
