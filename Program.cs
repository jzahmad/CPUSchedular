using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SchedulingAlgorithms
{
    public class TextData
    {
        public string Name { get; set; }
        public int ArrTime { get; set; }
        public int BurTime { get; set; }
        public int RemTime { get; set; }
        public int WaitTime { get; set; } // Only for NSJF
    }

    public class RRwait
    {
        public string Name { get; set; }
        public int WaitTime { get; set; }
        public int RemTime { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var data = new List<TextData>();
            using (var reader = new StreamReader("TaskSpec.txt"))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var parts = line.Split(',');
                    if (parts.Length == 3)
                    {
                        data.Add(new TextData
                        {
                            Name = parts[0],
                            ArrTime = int.Parse(parts[1]),
                            BurTime = int.Parse(parts[2]),
                            RemTime = int.Parse(parts[2])
                        });
                    }
                }
            }

            using (var output = new StreamWriter("Output.txt"))
            {
                FCFS(data, output);
                NSJF(data, output);
                PSJF(data, output);
                RR(data, output);

            }
        }

        private static void RR(List<TextData> data, StreamWriter output)
        {
            output.WriteLine("RR: ");
            var dataForRR = data.Select(d => new TextData
            {
                Name = d.Name,
                ArrTime = d.ArrTime,
                BurTime = d.BurTime,
                RemTime = d.BurTime
            }).ToList();

            var waitForRR = data.Select(d => new RRwait
            {
                Name = d.Name,
                WaitTime = 0,
                RemTime = d.BurTime
            }).ToList();

            int completed = 0;
            int currTime = 0;
            int index = 0;
            int timeQuantum = 4;

            while (completed < data.Count)
            {
                if (dataForRR[index].RemTime > 0)
                {
                    int executeTime = Math.Min(dataForRR[index].RemTime, timeQuantum);
                    output.WriteLine($"{dataForRR[index].Name}  {currTime}  {currTime + executeTime}");

                    dataForRR[index].RemTime -= executeTime;
                    currTime += executeTime;

                    for (int i = 0; i < data.Count; i++)
                    {
                        if (dataForRR[index].Name != waitForRR[i].Name && waitForRR[i].RemTime > 0)
                        {
                            waitForRR[i].WaitTime += executeTime;
                        }
                        else if (waitForRR[i].Name == dataForRR[index].Name)
                        {
                            waitForRR[i].RemTime -= executeTime;
                        }
                    }
                }

                if (dataForRR[index].RemTime == 0)
                {
                    completed++;
                }

                // Reset index when it exceeds the list size
                index = (index + 1) % dataForRR.Count;
            }

            int totalTime = 0;
            for (int i = 0; i < data.Count; i++)
            {
                output.WriteLine($"Waiting time {waitForRR[i].Name}, {waitForRR[i].WaitTime - data[i].ArrTime}");
                totalTime += waitForRR[i].WaitTime - data[i].ArrTime;
            }
            output.WriteLine($"Average Waiting Time: {(float)totalTime / data.Count}");
        }


        public static void FCFS(List<TextData> data, StreamWriter output)
        {
            output.WriteLine("FCFS: \n");
            int currTime = 0;
            int totalTime = 0;

            foreach (var item in data)
            {
                int waitTime = currTime - item.ArrTime;
                output.WriteLine($"{item.Name}  {currTime}  {currTime + item.BurTime}");
                currTime += item.BurTime;
                totalTime += waitTime;
            }

            output.WriteLine($"Average Waiting Time: {(float)totalTime / data.Count}");
        }


        public static void NSJF(List<TextData> data, StreamWriter output)
        {
            output.WriteLine("\nNSJF: \n");

            // Sort data by arrival time
            var sortedData = data.OrderBy(d => d.ArrTime).ToList();

            int currTime = 0;
            int totalWaitTime = 0;
            var waitTimes = new Dictionary<string, int>();

            // Initialize waiting times
            foreach (var item in sortedData)
            {
                waitTimes[item.Name] = 0;
            }

            while (sortedData.Any())
            {
                // Get the list of available jobs (those that have arrived by currTime)
                var availableJobs = sortedData.Where(d => d.ArrTime <= currTime).ToList();

                if (!availableJobs.Any())
                {
                    // If no job is available, advance time to the next arrival
                    currTime = sortedData.Min(d => d.ArrTime);
                    availableJobs = sortedData.Where(d => d.ArrTime <= currTime).ToList();
                }

                // Pick the job with the shortest burst time from available jobs
                var shortestJob = availableJobs.OrderBy(d => d.BurTime).First();

                // Calculate waiting time for the selected job
                shortestJob.WaitTime = currTime - shortestJob.ArrTime;
                waitTimes[shortestJob.Name] = shortestJob.WaitTime;

                // Print start and end times for the selected job
                output.WriteLine($"{shortestJob.Name}  {currTime}  {currTime + shortestJob.BurTime}");

                // Update current time
                currTime += shortestJob.BurTime;

                // Remove the job from the list of jobs
                sortedData.Remove(shortestJob);
            }

            foreach (var item in waitTimes)
            {
                output.WriteLine($"Waiting time {item.Key}: {item.Value}");
                totalWaitTime += item.Value;
            }

            output.WriteLine($"Average Waiting Time: {(float)totalWaitTime / data.Count}");
        }
        public static void PSJF(List<TextData> data, StreamWriter output)
        {
            output.WriteLine("PSJF:");
            int currentTime = 0;
            int completed = 0;
            int start = 0;
            int last = 0; // Initialize with -1 as no process has started
            int totalTimeOfBurst = 0;
            var waitTime = new List<int>(new int[data.Count]); // Initialize waiting time for all processes

            for (int i = 0; i < data.Count; i++)
            {
                totalTimeOfBurst += data[i].BurTime; // Sum up burst times
            }

            while (completed < data.Count)
            {
                int shortest = -1;
                int min_remaining_time = int.MaxValue;

                // Find the process with the shortest remaining time that has arrived
                for (int i = 0; i < data.Count; i++)
                {
                    if (data[i].ArrTime <= currentTime && data[i].RemTime < min_remaining_time && data[i].RemTime > 0)
                    {
                        shortest = i;
                        min_remaining_time = data[i].RemTime;
                    }
                }

                if (shortest == -1)
                {
                    currentTime++; // No process ready, increment the time
                }
                else
                {
                    if (data[shortest].Name != data[last].Name)
                    {
                        // Output the previous process execution details
                        output.WriteLine($"{data[last].Name}\t{start}\t{currentTime}");
                        start = currentTime;
                    }

                    last = shortest; // Update the last process being executed
                    data[shortest].RemTime--;
                    currentTime++;
                    if (currentTime == totalTimeOfBurst)
                    {
                        output.WriteLine($"{data[shortest].Name}\t{start}\t{currentTime}");
                    }

                    if (data[shortest].RemTime == 0)
                    {
                        completed++; // Increment completed processes count
                    }

                    // Update waiting time for other processes
                    for (int i = 0; i < data.Count; i++)
                    {
                        if (i != shortest && data[i].ArrTime < currentTime && data[i].RemTime > 0)
                        {
                            waitTime[i]++; // Increment waiting time if process is ready but not running
                        }
                    }
                }
            }

            // Print the waiting time for each process
            int totalWaitTime = 0;
            for (int i = 0; i < data.Count; i++)
            {
                output.WriteLine($"Waiting time {data[i].Name}: {waitTime[i]}");
                totalWaitTime += waitTime[i];
            }

            output.WriteLine($"Average Waiting Time: {(float)totalWaitTime / data.Count:F2}");
        }
    }
}