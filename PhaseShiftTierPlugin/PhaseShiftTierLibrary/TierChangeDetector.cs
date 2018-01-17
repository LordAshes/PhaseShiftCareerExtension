using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PhaseShiftTierLibrary
{
    /// <summary>
    /// Class for texting Tire Changes
    /// </summary>
    public class TierChangeDetector
    {
        /// <summary>
        /// Class for returning information about Tier changes
        /// </summary>
        public class TierInfo
        {
            /// <summary>
            /// Full path and name of the tier that was changed (ends in Tier_?.ini) where ? is a tier number
            /// (typically used to look up corresponding storyboard information after the tier change)
            /// </summary>
            public string name { get; set; }

            /// <summary>
            /// Name of the band that has accomplished the tier change
            /// (typically used to inject the band name into storyboard information)
            /// </summary>
            public string band { get; set; }

            /// <summary>
            /// Indicates the number of achievement stars needed to reach the tier
            /// </summary>
            public int requires { get; set; }

            /// <summary>
            /// Indicates the band's number of achievements stars (this should always be greater or equal to the requires parameter)
            /// </summary>
            public int accumulated { get; set; }
        }

        /// <summary>
        /// Lookup dictionary to map user dat files to band names
        /// </summary>
        public Dictionary<string, string> bands = new Dictionary<string, string>();

        /// <summary>
        /// Double dictionary indexed by Careeer and then by band (user dat) which holds the number of achievement stars for the band
        /// </summary>
        public Dictionary<string, Dictionary<string, int>> achievements = new Dictionary<string,Dictionary<string, int>>();

        /// <summary>
        /// Double dictionary indexed by Careeer and then by tier (tier ini) which holds the number of required achievement stars for the tier
        /// </summary>
        public Dictionary<string, Dictionary<string, int>> tiers = new Dictionary<string,Dictionary<string, int>>();

        /// <summary>
        /// Reference to the current band once detected (otherwise Unknown)
        /// </summary>
        public string currentBand = "Unknown";

        /// <summary>
        /// Reference to the current career once detected (otherwise Unknown)
        /// </summary>
        public string currentCareer = "Unknown";

        /// <summary>
        /// Reference to the current tier once detected (otherwise Unknown)
        /// </summary>
        public string currentTier = "Unknown";

        /// <summary>
        /// Boolean holding the optimize selection. If true, other careers and bands are remove once the current career, band and tiers are detected
        /// </summary>
        public bool optimizeOnceBandCareerDetected = true;

        /// <summary>
        /// Holds the time of the last check. Can be used as a heartbeat to determine that the detector is active
        /// </summary>
        public DateTime lastCheck;

        /// <summary>
        /// PhaseShiftStoryboard location use for testing
        /// </summary>
        public string root = "..";

        /// <summary>
        /// Local reference to the callback action which is used to post tier changes
        /// </summary>
        private Action<TierInfo> _callback = null;

        /// <summary>
        /// Local timer for determining when to do the next check
        /// </summary>
        private System.Timers.Timer check = null;


        /// <summary>
        /// Constructor for the TierChangeDetector
        /// </summary>
        /// <param name="callbackTierChange">Callback action which gets an instance of TierInfo when a tier change occures</param>
        /// <param name="instrument">Name of an instrument type whose tiers are to be used</param>
        public TierChangeDetector(Action<TierInfo> callbackTierChange, string instrument)
        {
            // Verify that the corresponding program is running from the correct location (a folder off of the main Phase Shift folder)
            if (!System.IO.File.Exists(root+@"\Phase_Shift.exe")) {Console.WriteLine("Incorrect Installation Of PhaseShiftStorybard.\r\n\r\nPhaseShiftStoryboard is to be installed in\r\n"+root); Environment.Exit(1); }
            // Verify that a valid Tier Instrument type has been provided
            if (!"BASS,GUITAR,BRUMS,KEYS".Contains(instrument.ToUpper())) { Console.WriteLine("Tier Intstrument '" + instrument + "' does not exist"); Environment.Exit(1); }
            // Assign local refernece to the provided callback action
            this._callback = callbackTierChange;
            // Initialize the initial value of bands and achievements
            InitializeBandFiles(bands, achievements);
            // Initialize the initial value of tiers
            ProcessTierFiles(instrument, tiers);
        }

        /// <summary>
        /// Constructor for the TierChangeDetector
        /// </summary>
        /// <param name="callbackTierChange">Callback action which gets an instance of TierInfo when a tier change occures</param>
        /// <param name="instrument">Name of an instrument type whose tiers are to be used</param>
        /// <param name="rootPhaseShiftFolder">Root folder of Phase Shift</param>
        public TierChangeDetector(Action<TierInfo> callbackTierChange, string instrument, string rootPhaseShiftFolder)
        {
            root = rootPhaseShiftFolder;
            // Verify that the corresponding program is running from the correct location (a folder off of the main Phase Shift folder)
            if (!System.IO.File.Exists(root + @"\Phase_Shift.exe")) { Console.WriteLine("Incorrect Installation Of PhaseShiftStorybard.\r\n\r\nPhaseShiftStoryboard is to be installed in\r\n" + root); Environment.Exit(1); }
            // Verify that a valid Tier Instrument type has been provided
            if (!"BASS,GUITAR,BRUMS,KEYS".Contains(instrument.ToUpper())) { Console.WriteLine("Tier Intstrument '" + instrument + "' does not exist"); Environment.Exit(1); }
            // Assign local refernece to the provided callback action
            this._callback = callbackTierChange;
            // Initialize the initial value of bands and achievements
            InitializeBandFiles(bands, achievements);
            // Initialize the initial value of tiers
            ProcessTierFiles(instrument, tiers);
        }

        /// <summary>
        /// Method for starting tier change detection
        /// </summary>
        /// <param name="checkRate">The rate, in miliseconds, at which the tier change is performed</param>
        /// <param name="optimizeOnceBandCareerDetected">Indication of optimization should be used</param>
        public void Start(int checkRate, bool optimizeOnceBandCareerDetected = true)
        {
            // Assign local reference to the provided optimization setting
            this.optimizeOnceBandCareerDetected = optimizeOnceBandCareerDetected;
            // Create a timer set to the provided check rate
            check = new System.Timers.Timer(checkRate);
            // Register a event handler for timer elapsed which will perform the check
            check.Elapsed += (s,e) => PerformCheck(s, e, achievements, tiers);
            // Start the check timer
            check.Start();
        }

        /// <summary>
        /// Method for stopping tier change detection
        /// </summary>
        public void Stop()
        {
            // Stop the check timer
            check.Stop();
            // Deregisetr the timer elapsed event handler
            check.Elapsed -= (s, e) => PerformCheck(s, e, achievements, tiers);
            // Dispose of the timer (since Start created a new timer)
            check.Dispose();
        }

        /// <summary>
        /// Timer elapsed event handler which performs the actual check
        /// </summary>
        /// <param name="sender">Timer. Not used</param>
        /// <param name="e">Timer Elapsed Args. Not used</param>
        /// <param name="achievements">Achievement double dictionary</param>
        /// <param name="tiers">Tiers double dictionary</param>
        private void PerformCheck(object sender, System.Timers.ElapsedEventArgs e, Dictionary<string,Dictionary<string, int>> achievements, Dictionary<string,Dictionary<string, int>> tiers)
        {
            // Stop check timer to prevent multiple checks if check takes long
            check.Stop();
            // Update last check time
            lastCheck = DateTime.UtcNow;
            // Generate a new double dictionary to hold the current achievements so that it can be compared to the previous double dictionary for changes
            Dictionary<string, Dictionary<string, int>> newAchievements = new Dictionary<string,Dictionary<string, int>>();
            // Populate the new double dictionary with current values
            ProcessBandFiles(newAchievements, achievements);
            // Run through all the achievement careers
            for(int c = 0; c < achievements.Count; c++)
            {
                // Run through all the achievement career bands
                for(int b = 0; b < achievements.ElementAt(c).Value.Count; b++)
                {
                    // Check to see if the achievements for each career & band have changed
                    if (newAchievements[achievements.ElementAt(c).Key][achievements.ElementAt(c).Value.ElementAt(b).Key] > achievements.ElementAt(c).Value.ElementAt(b).Value)
                    {
                        // Achievement value changed, update the current band reference
                        currentBand = bands[achievements.ElementAt(c).Value.ElementAt(b).Key];
                        // Achievement value changed, update the current career reference
                        currentCareer = achievements.ElementAt(c).Key;
                        // Consults all tiers for the matching career
                        foreach (KeyValuePair<string, int> reqs in tiers[achievements.ElementAt(c).Key])
                        {
                            // Check if the previous achievements were less than the tier requirements but the new achievements are equal or more
                            if((newAchievements[achievements.ElementAt(c).Key][achievements.ElementAt(c).Value.ElementAt(b).Key]>=reqs.Value)&&(achievements.ElementAt(c).Value.ElementAt(b).Value<reqs.Value))
                            {
                                // Tier change occured. Trigger callback with a TierInfo instance relating this tier change information
                                _callback(new TierInfo() { name = reqs.Key, band = bands[achievements.ElementAt(c).Value.ElementAt(b).Key], requires = reqs.Value, accumulated = newAchievements[achievements.ElementAt(c).Key][achievements.ElementAt(c).Value.ElementAt(b).Key] });
                            }
                            // Store the current tier
                            if(newAchievements[achievements.ElementAt(c).Key][achievements.ElementAt(c).Value.ElementAt(b).Key] >= reqs.Value) { currentTier = reqs.Key; }
                        }
                        // Assign the new achievements value to the current achievements value so that we don't trip a tier change multiple time
                        achievements[achievements.ElementAt(c).Key][achievements.ElementAt(c).Value.ElementAt(b).Key] = newAchievements[achievements.ElementAt(c).Key][achievements.ElementAt(c).Value.ElementAt(b).Key];
                    }
                }
            }
            // Optimize if career and band known and the optimize settings is turned on
            if ((currentCareer != "Unknown") && (currentBand != "Unknown") && (optimizeOnceBandCareerDetected == true)) { Optimize(achievements, tiers); }
            // Restart check timer
            check.Start();
        }

        /// <summary>
        /// Method to remove careers and tiers from their respective double dictionaries if they do not match the current career
        /// </summary>
        /// <param name="achievements">Achievements double dictionary to be optimized</param>
        /// <param name="tiers">Tiers double dictionary to be optimized</param>
        public void Optimize(Dictionary<string, Dictionary<string, int>> achievements, Dictionary<string, Dictionary<string, int>> tiers)
        {
            List<string> careersToBeRemoved = new List<string>();
            List<string> bandsToBeRemoved = new List<string>();
            // Cycle through all the carrers in achievements
            foreach (KeyValuePair<string, Dictionary<string, int>> career in tiers)
            {
                // Check id the achivement career matches the current career
                if (career.Key != currentCareer)
                {
                    // If the achievement career does not match the current career, remove it
                    careersToBeRemoved.Add(career.Key);
                }
                else
                {
                    // Career may not be in achievements if there are no bands or if bands have not achieved anything
                    if (achievements.ContainsKey(career.Key))
                    {
                        // Cycle through all the achievement bands in the career matching the current career
                        foreach (KeyValuePair<string, int> band in achievements[career.Key])
                        {
                            // Remove bands within current careers which do not matching the current band
                            if (bands[band.Key] != currentBand) { bandsToBeRemoved.Add(band.Key); }
                        }
                    }
                }
            }
            // Removed career and bands identified as not matching current career or current band
            foreach (string career in careersToBeRemoved)
            {
                if (achievements.ContainsKey(career)) { achievements.Remove(career); }
                tiers.Remove(career);
            }
            foreach (string band in bandsToBeRemoved)
            {
                achievements[currentCareer].Remove(band);
            }
        }

        /// <summary>
        /// Method to initialize the initial bands and achievements double dictionaries.
        /// This method is used only once at the beginning because it anumerates bands and achievements based on the folder files.
        /// Actual tier checks do not use this method because they verify achievements based on entries in the achievements double dictionary.
        /// This allows easy implementation of the optimize function because once the other careers and bands are removed from the double dictionary
        /// they will no longer be check (as opposed to this method which would still check them because it is based on the presence of user dat files)
        /// </summary>
        /// <param name="bands"></param>
        /// <param name="achievements"></param>
        private void InitializeBandFiles(Dictionary<string, string> bands, Dictionary<string, Dictionary<string, int>> achievements)
        {
            // Read each career in careers folder
            foreach (string career in System.IO.Directory.EnumerateDirectories(root+@"\careers").ToArray())
            {
                // Extract the career name from the folder (career.ini file is not used)
                string careerName = career.Substring(career.LastIndexOf("\\")+1);
                // Cycle through each user dat file (band) within the career
                foreach (string careerBand in System.IO.Directory.EnumerateFiles(@career, "user_*.dat").ToArray())
                {
                    // Process the user dat file and add the contents to bands and achievements
                    ProcessBandFile(careerName, careerBand, bands, achievements);
                }
            }
        }

        /// <summary>
        /// This method is used to populate an achievements double dictionary with the current achievements.
        /// Unlike the initialization method which uses the presence of user dat files to determine which user dat files to check (i.e. all user dat file)
        /// this method only updates user dat files corresponding to users (bands) provided in the supporting achievements double dictionary.
        /// In this manner, once the current career and band is determine, other careers and bands can be remove from the supporting double dictionary
        /// and, thereby, be no longer check (i.e. optimized checking).
        /// </summary>
        /// <param name="achievements">The double dictionary to receive the current achievements</param>
        /// <param name="prevAchievements">The reference double dictionary which holds the previous achievements</param>
        private void ProcessBandFiles(Dictionary<string, Dictionary<string, int>> achievements, Dictionary<string, Dictionary<string, int>> prevAchievements)
        {
            // Cycle through each achievement careers in the previous achievements double dictionary
            foreach (KeyValuePair<string, Dictionary<string, int>> career in prevAchievements)
            {
                // Cycle through each achievement band in the selected achievement career in the previous achievements double dictionary
                foreach (KeyValuePair<string, int> band in career.Value)
                {
                    // Process the coresponding user dat file to populate the new achievements double dictionary
                    // (band is set to null since the new achievements does not require an updated band lookup table)
                    ProcessBandFile(career.Key, band.Key, null, achievements, prevAchievements);
                }
            }
        }

        /// <summary>
        /// This method performs the actual user dat file processing to extract the band name and the band's achievements
        /// (used by both the initialization process and then successive checks)
        /// </summary>
        /// <param name="careerName">String representing the career name</param>
        /// <param name="careerBand">String representing the band user dat file</param>
        /// <param name="bands">Dictionary of band lookups (maps user dat files to band name) to be populated (unless null)</param>
        /// <param name="achievements">Double dictionary of achievements to be populated</param>
        /// <param name="defaultAchievements">Double dictionary of previous achievements used to populate new achievements if file is not accessible</param>
        private void ProcessBandFile(string careerName, string careerBand, Dictionary<string, string> bands, Dictionary<string,Dictionary<string, int>> achievements, Dictionary<string, Dictionary<string, int>> defaultAchievements = null)
        {
            try
            {
                //
                // Notice:
                // Since the source code for Phase Shift has not been made public the information herein was reverse engineered.
                // It seems to be consitantly working in the cases that I have tried but could contain errors under certain conditions.
                //
                // Create a bytes buffer and populate it with the bytes of the corresponding user dat file
                byte[] bytes = System.IO.File.ReadAllBytes(careerBand);
                // The band name seems to start at the 40th offset
                int endPos = 40;
                // Look for a 0 value character to identify the end of the band name
                for (; endPos < bytes.Count(); endPos++) { if(bytes[endPos] == 2) { break; } }
                // Initialize the bands's achievement stars count to 0
                int stars = 0;
                // Create a storage variable for the song id. This is not current used but could be useful for future projects.
                string songId = "";
                // Apply ASCII encoding to the band name bytes to generate the band name as a string
                string bandName = Encoding.ASCII.GetString(bytes, 39, endPos - 40 + 1);
                // Advance to the first song's achievement star count
                endPos = endPos + 16;
                // Cycle through all the songs in the user dat file
                while (endPos < bytes.Count())
                {
                    // Increase the achievement star count by the song's stars achievement 
                    stars = stars + bytes[endPos];
                    // Advance to the song id
                    endPos = endPos + 20;
                    // Apply ASCII encoding to the song id to generate the sonf id as a string. Song id seems to have a fixed 32 byte length
                    songId = Encoding.ASCII.GetString(bytes, endPos, 32);
                    // Advance to the first song's achievement star count (if present)
                    endPos = endPos + 48;
                }
                // Add the band name to the bands lookup if the bands double dictionary is provided
                // (Only used during initialize since the bands list does not change)
                if (bands != null) { bands.Add(careerBand, bandName); }
                // Achievement should never be null but we check just in case someone is calling the method incorrectly
                if (achievements != null)
                {
                    // Ensure that the double dictionary has an entry for the corresponding career
                    if (!achievements.ContainsKey(careerName)) { achievements.Add(careerName, new Dictionary<string, int>()); }
                    // Add the current band with its current achievements to the achievements double dictionary
                    achievements[careerName].Add(careerBand, stars);
                }
            }
            catch(Exception)
            {
                // File might have been lock. Try again later.
                if (defaultAchievements != null) { achievements[careerName].Add(careerBand, defaultAchievements[careerName][careerBand]); }
            }
        }

        /// <summary>
        /// Method for updating tier double dictionary information
        /// </summary>
        /// <param name="instrument">The instrument type for which tier information should be processed</param>
        /// <param name="tiers">Tier double dictionary to be populated with tier information</param>
        private void ProcessTierFiles(string instrument, Dictionary<string,Dictionary<string, int>> tiers)
        {
            // Cycle through all the careers in the careers folder
            foreach (string career in System.IO.Directory.EnumerateDirectories(root+@"\careers").ToArray())
            {
                // Extact the career name from the folder (career.ini file is not used)
                string careerName = career.Substring(career.LastIndexOf("\\")+1);
                // Cycle through al the tier ini files in the given instrument folder for the specified career
                foreach (string careerTier in System.IO.Directory.EnumerateFiles(career + "\\" + instrument, "tier_*.ini").ToArray())
                {
                    // Process the tier ini file
                    ProcessTierFile(careerName, careerTier, tiers);
                }
            }
        }

        /// <summary>
        /// Method for performing actual tier ini file processing
        /// </summary>
        /// <param name="careerName">String representation of the selected career</param>
        /// <param name="careerTier">String representation of the tier file to be processed</param>
        /// <param name="tiers">Tiers double dictionary to be populated</param>
        private void ProcessTierFile(string careerName, string careerTier, Dictionary<string, Dictionary<string, int>> tiers)
        {
            // Read contents of the tier ini file
            string contents = System.IO.File.ReadAllText(careerTier);
            // Parse out the tier name. Current not used but a tier loopup table could be added
            // (similar to the bands lookup table) to translate tier file to tier names
            string tierName = contents.Substring(contents.IndexOf("name = \"") + 8);
            tierName = tierName.Substring(0, tierName.IndexOf("\""));
            // Parse tier requirements
            string tierReqs = contents.Substring(contents.IndexOf("unlock = \"") + 10);
            tierReqs = tierReqs.Substring(0, tierReqs.IndexOf("\""));
            // Tier double dictionary should never be null but we check just in case someone calls this method incorrectly
            if (tiers != null)
            {
                // Ensure that a entry for the career exists in the tier double dictionary
                if (!tiers.ContainsKey(careerName)) { tiers.Add(careerName, new Dictionary<string, int>()); }
                // Add the tier requirements to the tier double dictionary
                tiers[careerName].Add(careerTier, int.Parse(tierReqs));
            }
        }
    }
}
