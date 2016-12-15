using System;
using System.IO;
using DesignTimeMapper.MapperGeneration;

namespace DesignTimeMapper.CommandLine
{
    class Program
    {
        private const int ErrorSuccess = 0;
        private const int ErrorInvalidFunction = 0x1;
        private const int ErrorFileNotFound = 0x2;
        private const int ErrorBadArguments = 0xA0;
        private const int ErrorInvalidCommandLine = 0x667;

        static int Main(string[] args)
        {
            if (args == null || args.Length != 2)
            {
                Console.Error.WriteLine("Error: DesignTimeMapper - Please provide a path to solution and a project name");
                return ErrorInvalidCommandLine;
            }
            
            try
            {
                var path = args[0];

                if (!File.Exists(path))
                {
                    Console.Error.WriteLine($"Error: Could not find .sln file at path '{path}'");
                    return ErrorFileNotFound;
                }

                var projectName = args[1];
                if (string.IsNullOrWhiteSpace(projectName))
                {
                    Console.Error.WriteLine("Error: DesignTimeMapper - Project name cannot be empty");
                    return ErrorInvalidCommandLine;
                }

                var mapper = new Mapper();
                mapper.Map(path, projectName).Wait();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Error: Unexpected exception: {e}");

                return ErrorInvalidFunction;
            }

            Console.WriteLine("Design Time Mapper - mapping completed successfully");
            return ErrorSuccess;
        }
    }
}
