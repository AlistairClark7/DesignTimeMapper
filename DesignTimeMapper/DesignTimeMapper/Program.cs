using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DesignTimeMapper.Engine;
using DesignTimeMapper.Engine.Settings;
using Newtonsoft.Json;

namespace DesignTimeMapper
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
            if (args == null || args.Length != 1)
            {
                Console.Error.WriteLine("Please provide a path to the Design Time Mapper settings file");
                return ErrorInvalidCommandLine;
            }
            
            try
            {
                var path = args[0];

                if (!File.Exists(path))
                {
                    Console.Error.WriteLine($"Could not find Design Time Mapper settings at path '{path}'");
                    return ErrorFileNotFound;
                }
                    
                var settingsText = File.ReadAllText(path);
                var settings = JsonConvert.DeserializeObject<DtmSettings>(settingsText);

                if (settings == null)
                {
                    Console.Error.WriteLine($"Could not parse contents of settings at path '{path}'");   
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Unexpected exception: {e}");

                return ErrorInvalidFunction;
            }

            return ErrorSuccess;
        }
    }
}
