namespace TurnitTestExercise
{
    class Program
    {
        static string filePath = "times.txt";
        
        static void Main(string[] args)
        {
            filePath = args.Length > 1 && args[0] == "filename" ? args[1] : "times.txt"; //Get filePath from args

            var commands = new Dictionary<string, Action<string[]>>
            {
                { "add", parameters => AddTime(parameters, "Usage: add <start time><end time> (example: add 13:1514:00)") },
                { "run", parameters => ProcessTimeTable() },
                { "file", parameters => SetFilePath(parameters, "Usage: file <file path>") },
                { "list", parameters => ListBreakTimes() },
                { "help", parameters => ShowHelp() },
                { "exit", parameters => ExitApplication() }
            };

            ProcessTimeTable();
            Console.WriteLine("Type 'help' to see the list of commands");

            while (true)
            {
                Console.Write("> ");
                var input = (Console.ReadLine() ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (input.Length == 0) continue;

                var command = input[0].ToLower();
                var parameters = input.Length > 1 ? input[1..] : Array.Empty<string>();

                if (commands.ContainsKey(command))
                {
                    try
                    {
                        commands[command](parameters);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"Unknown command: {command}. Type 'help' for a list of commands.");
                }
            }
        }

        static void ProcessTimeTable()
        {
            var times = ReadTimesFromFile();
            if (times.Count > 0)
            {
                CalculateBusiestPeriod(times);
            }
            else
            {
                Console.WriteLine("No break times found.");
            }
        }

        static List<Tuple<TimeSpan, TimeSpan>> ReadTimesFromFile()
        {
            var times = new List<Tuple<TimeSpan, TimeSpan>>();

            if (File.Exists(filePath))
            {
                var lines = File.ReadAllLines(filePath);
                foreach (var line in lines)
                {
                    string startTimeStr = line[..5];
                    string endTimeStr = line[5..];
                    if (TimeSpan.TryParse(startTimeStr, out TimeSpan startTime) && TimeSpan.TryParse(endTimeStr, out TimeSpan endTime))
                    {
                        Tuple<TimeSpan, TimeSpan> timeTuple = new Tuple<TimeSpan, TimeSpan>(startTime, endTime);
                        times.Add(timeTuple);
                    }
                }
            }

            return times;
        }

        //Look for periods with the most overlapping breaks. Currently supports only one period, outputs the first one it found, could add more with a list
        static void CalculateBusiestPeriod(List<Tuple<TimeSpan, TimeSpan>> breakPeriods)
        {
            var breakEvents = new List<Tuple<TimeSpan, int>>();

            //Populate List: Item1 = Time of start or end
            foreach (var breakPeriod in breakPeriods)   
            {
                breakEvents.Add(Tuple.Create(breakPeriod.Item1, 1));    //Item2==1 (Start time)
                breakEvents.Add(Tuple.Create(breakPeriod.Item2, -1));   //Item2==-1 (End time)
            }

            breakEvents.Sort((x, y) =>
            {
                int result = x.Item1.CompareTo(y.Item1);    //Sort events by time
                if (result == 0)
                {
                    result = x.Item2.CompareTo(y.Item2);    //If times are the same compare by type.
                }
                return result;
            });
            
            int currentBreaks = 0;  //Current number of active breaks
            int maxBreaks = 0;          //All time highest number of breaks
            TimeSpan maxStart = TimeSpan.MinValue;  //Start time of busiest period
            TimeSpan maxEnd = TimeSpan.MinValue;    //End time of busiest period
            bool busiestPeriod = false;  //Are we currently tracking the busiest period?

            var busiestPeriods = new List<Tuple<TimeSpan, int, TimeSpan>>();
            TimeSpan previousStart = TimeSpan.MinValue;
            
            foreach (var ev in breakEvents)
            {
                currentBreaks += ev.Item2;
                if (ev.Item2 == 1) // Start of a break
                {
                    if (currentBreaks > maxBreaks)  //Busiest period so far
                    {
                        maxBreaks = currentBreaks;
                        maxStart = ev.Item1;    
                        busiestPeriod = true;
                        busiestPeriods.Clear();
                    }
                    else if (currentBreaks == maxBreaks)  //Equal to the busiest period so far
                    {
                        maxBreaks = currentBreaks;
                        maxStart = ev.Item1;    
                        busiestPeriod = true;
                    }
                    previousStart = ev.Item1;   //Set previous start to carry over for if next iteration is end of break
                }
                else // End of a break
                {
                    if (currentBreaks == maxBreaks)
                    {
                        maxEnd = ev.Item1;
                    }
                    else if (currentBreaks <= maxBreaks && busiestPeriod)    //End of busiest period, currentBreaks < maxBreaks
                    {
                        busiestPeriods.Add(Tuple.Create(previousStart, maxBreaks, ev.Item1));
                        // Update the end time and reset currentStart
                        maxEnd = ev.Item1;
                        busiestPeriod = false;
                    }
                }
            } 
            
            Console.WriteLine($"Current File: {filePath}, Total Drivers: {breakPeriods.Count}");
            foreach (var period in busiestPeriods)
            {
                Console.WriteLine($"Busiest period: {period.Item1:hh\\:mm}-{period.Item3:hh\\:mm} with {period.Item2} drivers on break. Free drivers: {breakPeriods.Count - maxBreaks}");
            }
            
            // Console.WriteLine($"Busiest period: {maxStart:hh\\:mm}-{maxEnd:hh\\:mm} with {maxBreaks} drivers on break. Free drivers: {breakPeriods.Count - maxBreaks}");
        }

        static void AddTime(string[] parameters, string usage)  //Add new entry to break time table
        {
            if (parameters.Length == 1 && ValidateTimeEntry(parameters[0]).Item1)
            {
                try
                {
                    File.AppendAllText(filePath, parameters[0] + Environment.NewLine);
                    Console.WriteLine($"Entry added: {parameters[0]}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error writing to file: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"Invalid break time: {parameters[0]}");
                Console.WriteLine(ValidateTimeEntry(parameters[0]).Item2);
                Console.WriteLine(usage);
            }
            ProcessTimeTable();
        }

        static Tuple<bool, string> ValidateTimeEntry(string entry)     //Verify that an added break time entry is in the correct format.
        {
            if (entry.Length != 10) return new Tuple<bool, string>(false, "Time entry was not the right length.");

            string startTimeStr = entry.Substring(0, 5);
            string endTimeStr = entry.Substring(5, 5);
            
            if (TimeSpan.TryParse(startTimeStr, out TimeSpan startTime) && TimeSpan.TryParse(endTimeStr, out TimeSpan endTime))
            {
                if (startTime < endTime)
                {
                    return new Tuple<bool, string>(true, "New time entry was added to the table.");
                }
                else
                {
                    return new Tuple<bool, string>(false, "The break start time must be smaller than the end time.");
                }
            }
            else 
            {
                return new Tuple<bool, string>(false, "Entry was not recognized as valid time values.");
            }
            
        }

        static void SetFilePath(string[] parameters, string usage)
        {
            if (parameters.Length == 1)
            {
                filePath = parameters[0];
                Console.WriteLine($"File path set to: {filePath}");
            }
            else
            {
                Console.WriteLine(usage);
            }
            ProcessTimeTable();
        }

        static void ListBreakTimes()    //Extra functionality for listing all existing break times in the console.
        {
            var times = ReadTimesFromFile();
            if (times.Count > 0)
            {
                foreach (var time in times)
                {
                    Console.WriteLine($"{time.Item1:hh\\:mm}-{time.Item2:hh\\:mm}");
                }
                CalculateBusiestPeriod(times);
            }
        }

        static void ShowHelp()
        {
            Console.WriteLine("Available commands:");
            Console.WriteLine("add <start time><end time>     - Adds a break time");
            Console.WriteLine("run                            - How many drivers are on break?");
            Console.WriteLine("file                           - Set file path");
            Console.WriteLine("list                           - Lists all break times");
            Console.WriteLine("help                           - Shows this help message");
            Console.WriteLine("exit                           - Exits the application");
        }

        static void ExitApplication()
        {
            Console.WriteLine("Exiting the application.");
            Environment.Exit(0);
        }
    }
}

