using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sharp7;
using System.IO;


 
namespace EvaServer
{
    class Program
    {
        static S7Client _s7Plc = new S7Client();
        static byte[] db43Buffer = new byte[28];
        static byte[] db43WBuffer = new byte[28];
        static string fileStatystyki = (@"\\ma01\Firma\programy\EvaVisual\Baza\statystyki.txt");
        static bool ResetBit = false;
        static int s7Connect()
        {
            _s7Plc.Disconnect();
            int result = _s7Plc.ConnectTo("10.17.14.211", 0, 0);
            return result;
         }
        static void updateBase(int index, string Date, string FirstTimeRunning, string TimeWorkDay,  string TimeRunning, string LongTimeElement,  uint CounterElements)
        {
            if (File.Exists(fileStatystyki))
            {
                using (StreamWriter file = new StreamWriter(Path.Combine(fileStatystyki), true))
                {
                    file.WriteLine( index + "\t" + Date + "\t" + FirstTimeRunning + "\t" + TimeWorkDay + "\t" + TimeRunning + "\t" + LongTimeElement + "\t" + CounterElements);
                    file.Close();
                }

            }
        }
        static int Wczytajbaze()
        {
            int CountLine = 0;


            if (File.Exists(fileStatystyki))
            {
                
                FileStream file = File.Open(fileStatystyki, FileMode.Open, FileAccess.Read);
                file.Close();
                foreach (var line in File.ReadLines(fileStatystyki))
                {
                    var data = line.Split('\t');
                    //  ListView_Statystyki.Items.Add(new ListViewItem(data));
                    CountLine++;
                }
               
            }
            return CountLine;
        }








        static void Main(string[] args)
        {
            int delay = 10000 ;
            int countDisconnect = 0;
            int index = 0;
            const int START_INDEX = 0;

            
           // Console.BackgroundColor = ConsoleColor.White;
           // Console.Clear();

            while (true)
            {
                if (s7Connect() == 0)
                {
                    Console.WriteLine("Status servera: " + "Nawiązano połączenie\n");
                    int readResult = _s7Plc.DBRead(43, 0, db43Buffer.Length, db43Buffer);
                    Console.WriteLine("Ilość TimeOut: " + countDisconnect +"\n");
                    DateTime s7SaveDay = S7.GetDTLAt(db43Buffer, 2);  //("DTL#yyyy-MM-dd-HH:mm:ss.fffffff");
                    string s7Data = s7SaveDay.ToString("dd.MM.yyyy");
                    string s7FirstTimeRunning = s7SaveDay.ToString("HH:mm:ss");
                    Console.WriteLine("Moment zerowania parametrów: " + s7SaveDay);
                    DateTime s7TimeWorkDay = S7.GetTODAt(db43Buffer, 14);  //("DTL#yyyy-MM-dd-HH:mm:ss.fffffff");
                    string CzasPracyMaszyny= s7TimeWorkDay.ToString("HH:mm:ss");
                    Console.WriteLine("Czas pracy maszyny: " + CzasPracyMaszyny);

                    DateTime s7TimeRunning = S7.GetTODAt(db43Buffer, 24);  //("DTL#yyyy-MM-dd-HH:mm:ss.fffffff");
                    string TimeEffect = s7TimeRunning.ToString("HH:mm:ss");
                    Console.WriteLine("Efektywny czas pracy: " + TimeEffect);

                    DateTime s7LongTimeNextElement = S7.GetTODAt(db43Buffer, 18);  //("DTL#yyyy-MM-dd-HH:mm:ss.fffffff");
                    string TimeElements = s7LongTimeNextElement.ToString("HH:mm:ss");
                    Console.WriteLine("Najdłuższy czas między elementami: " + TimeElements);

                    uint s7CounterElements = S7.GetUIntAt(db43Buffer, 0);
                    Console.WriteLine("Dobowa ilość elementów: " + s7CounterElements);
                    bool s7DataOk = S7.GetBitAt(db43Buffer, 22,0);
                    System.Threading.Thread.Sleep(1000);
                    if (s7DataOk == true)
                    {
                        //   updateBase(index,s7Data,s7FirstTimeRunning,CzasPracyMaszyny,TimeEffect,TimeElements, s7CounterElements);
                        int writeResult = _s7Plc.DBWrite(43, 0, db43WBuffer.Length, db43WBuffer);
                        index = (Wczytajbaze());
                        index++;
                        Console.WriteLine("Update bazy danych... " );
                        updateBase(index, s7Data,s7FirstTimeRunning, TimeEffect, CzasPracyMaszyny, TimeElements,s7CounterElements);
                        // public static void SetBitAt(ref byte[] Buffer, int Pos, int Bit, bool Value)
                        S7.SetBitAt(ref db43WBuffer,22,0, false);
                        Console.WriteLine("Wartość Index: " + index);
                    }
                }
               else
                {
                    
                    Console.WriteLine("Status servera: " + "Brak połączenia\n");
                    countDisconnect ++;
                    Console.WriteLine("Ilość TimeOut: " + countDisconnect);
                }
                _s7Plc.Disconnect();
                System.Threading.Thread.Sleep(delay);
                Console.Clear();


            }
            while (Console.ReadKey().Key != ConsoleKey.Enter) { }
        }
    }
}
