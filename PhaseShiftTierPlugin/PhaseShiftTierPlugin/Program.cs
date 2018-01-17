using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace PhaseShiftTierPlugin
{
    class Program
    {
        public static string Notification = "";
        public static PhaseShiftTierLibrary.TierChangeDetector pstd = null;

        private static string consumer = "";
        private static string folder = "..";

        /// <summary>
        /// Main entry into application
        /// </summary>
        /// <param name="args">0: String representing the instrument type for which tiers should be used</param>
        static void Main(string[] args)
        {
            // Verify specification of instrument type
            if (!args.Any()) { Console.WriteLine("Missing Instrument Type Argument\r\n\r\nSyntax: PhaseShiftStoryboard IntrumentType\r\n\r\nExample: PhaseShiftStorybord guitar"); }
            if (args.Count() >= 2) { consumer = args[1]; }
            if (args.Count() >= 3) { folder = args[2]; }
            // Create an instance of the tier change detector
            pstd = new PhaseShiftTierLibrary.TierChangeDetector(callback, args[0], folder);
            // Start the tier change detector with a check every 10 seconds
            pstd.Start(10000,true);
            // Create a time to update diagnostic date every 5 seconds
            System.Timers.Timer check = new System.Timers.Timer(5000);
            // Regster evern handler for updating diagnostic data
            check.Elapsed += Check_Elapsed;
            // Start diagnostic data timer
            check.Start();
            // Prevent application from ending
            Console.ReadLine();
            // Stop diagnostic data updates
            check.Stop();
            // Stop tier change detection
            pstd.Stop();
        }

        /// <summary>
        /// Event handler for updating diagnostic data
        /// </summary>
        /// <param name="sender">Timer. Unused</param>
        /// <param name="e">Timer Elapsed Args. Unused</param>
        private static void Check_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            // Clear diagnostic data
            Console.Clear();
            foreach (KeyValuePair<string,Dictionary<string,int>> career in pstd.achievements)
            {
                foreach (KeyValuePair<string, int> band in career.Value)
                {
                    Console.WriteLine(pstd.bands[band.Key]+" : "+career.Key + " : "+band.Value);
                }
            }
            Console.WriteLine();
            Console.WriteLine("Current Band: "+pstd.currentBand);
            Console.WriteLine("Current Career: " + pstd.currentCareer);
            Console.WriteLine("Current Tier: " + pstd.currentTier);
            Console.WriteLine();
            Console.WriteLine("WriteOut: "+DateTime.UtcNow);
            Console.WriteLine("Update: "+pstd.lastCheck);
            Console.WriteLine();
            Console.WriteLine(Notification);
        }

        /// <summary>
        /// Cllaback when tier changes
        /// </summary>
        /// <param name="obj">TierInfo regarding the tier change</param>
        private static void callback(PhaseShiftTierLibrary.TierChangeDetector.TierInfo obj)
        {
            // Update tier change notification
            Notification = "("+DateTime.UtcNow+") Band " + obj.band + " Has Achieved " + obj.accumulated + " Stars Which Meets Tier " + obj.name + " Which Needs " + obj.requires;
            if (consumer != "")
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = consumer,
                    WorkingDirectory = Environment.CurrentDirectory,
                    Arguments = "\"" + obj.name + "\" \"" + obj.band+"\" " + obj.accumulated +" "+obj.requires,
                });
            }
        }
    }
}
