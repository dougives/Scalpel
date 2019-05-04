using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using NUnit.Framework;
using Arbitrary.Scalpel.Compiler;

namespace Arbitrary.Scalpel.Compiler.Tests
{
    public class ScalpelParserTests
    {
        void WaitForDebugger()
        {
            var test_process = Process.GetCurrentProcess();
            TestContext.Progress.WriteLine(
                $"Waiting for debugger to attach: " +
                $"{test_process.ProcessName}:{test_process.Id}");
            while(!Debugger.IsAttached) 
                Thread.Sleep(500);
        }

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void ParseTest()
        {
            WaitForDebugger();
            var syntax = ScalpelParser.ParseFromFile("test.ask");
            Assert.Pass();
        }
    }
}