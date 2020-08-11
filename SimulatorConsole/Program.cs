using System;

namespace SimulatorConsole
{
    public class Program
    {
        
        static void Main(string[] args) //0- testName, 1-replayMultipler
        {
            var sim = new Simulator();
            var testName = args[0];
            var testGroupName = args[1];
            var replayMultiplier = Convert.ToInt32(args[2]);
            var execsPerMinute = Convert.ToInt32(args[3]);
            var maxRequests = Convert.ToInt32(args[4]);
            var obj = Activator.CreateInstance("SimulatorConsole", testName);
            IProcedureTest testType = (IProcedureTest)obj.Unwrap();

            sim.Initialize(testType, testGroupName);
            sim.StartTest(replayMultiplier, execsPerMinute, maxRequests);
           
        }

       
    }

}