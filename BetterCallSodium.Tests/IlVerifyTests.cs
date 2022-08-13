using AOT;
using System;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace BetterCallSodium.Tests
{
    public class IlVerifyTests
    {
        private readonly ITestOutputHelper _output;

        public IlVerifyTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void VerifyTest()
        {
            int exitCode = ProcessUtils.GetProcessOutput("dotnet", "tool restore", out string output, out string error);

            if (!string.IsNullOrWhiteSpace(output))
                _output.WriteLine($"Restore output: {output}");
            if (!string.IsNullOrWhiteSpace(error))
                _output.WriteLine($"Restore error: {error}");

            Assert.Equal(0, exitCode);

            string references = "";
            foreach (Assembly referencedAssembly in AppDomain.CurrentDomain.GetAssemblies().Where(assembly => !assembly.IsDynamic))
                references += $" -r \"{referencedAssembly.Location}\"";

            string arguments = $"ilverify \"{typeof(MonoPInvokeCallbackAttribute).Assembly.Location}\" -s \"{typeof(object).Assembly.GetName().Name}\" {references}";
            exitCode = ProcessUtils.GetProcessOutput("dotnet", arguments, out output, out error);

            if (!string.IsNullOrWhiteSpace(output))
                _output.WriteLine($"IlVerify output: {output}");
            if (!string.IsNullOrWhiteSpace(error))
                _output.WriteLine($"IlVerify error: {error}");

            Assert.Equal(0, exitCode);
        }
    }
}
