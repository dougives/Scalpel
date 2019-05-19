using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using NUnit.Framework;
using Arbitrary.Scalpel.Compiler;
using Arbitrary.Scalpel.Dissection;

namespace Arbitrary.Scalpel.Compiler.Tests
{
    public class ScalpelCompilerTests
    {
        void WaitForDebugger()
        {
            var test_process = Process.GetCurrentProcess();
            TestContext.Progress.WriteLine(
                $"Waiting for debugger to attach: " +
                $"{test_process.ProcessName}:{test_process.Id}");
            while (!Debugger.IsAttached) 
                Thread.Sleep(500);
        }

        private static readonly byte[] EthernetTestPacket = new byte[]
        {
            0x02, 0x42, 0xac, 0x12, 0x00, 0x02, 0x02, 0x42,
            0x5e, 0x73, 0x5a, 0xb1, 0x08, 0x00, 0x45, 0x00,
            0x00, 0x40, 0x00, 0x00, 0x40, 0x00, 0x24, 0x06,
            0xe8, 0xa2, 0xc0, 0x00, 0x02, 0x01, 0xac, 0x12,
            0x00, 0x02, 0x0a, 0xa9, 0x1b, 0x59, 0x61, 0x32,
            0xc9, 0x31, 0x00, 0x00, 0x00, 0x00, 0xb0, 0x02,
            0xff, 0xff, 0x30, 0x27, 0x00, 0x00, 0x02, 0x04,
            0x05, 0x08, 0x01, 0x03, 0x03, 0x06, 0x01, 0x01,
            0x08, 0x0a, 0x01, 0x75, 0x47, 0x90, 0x00, 0x00,
            0x00, 0x00, 0x04, 0x02, 0x00, 0x00,
        };

        [SetUp]
        public void Setup()
        {
            // WaitForDebugger();
        }

        [Test]
        public void KernelCompilerTest()
        {
            // WaitForDebugger();
            var obj = ScalpelKernel.CompileFromFile("test.ask");
            Assert.Pass();
        }

        [Test]
        public void BuildProgramTest()
        {
            // WaitForDebugger();
            var program = ScalpelProgram.FromKernelFiles("test.ask");
            Assert.Pass();
        }

        [Test]
        public void CycleProgramTest()
        {
            WaitForDebugger();
            var program = ScalpelProgram.FromKernelFiles("test.ask");
            var packet = Packet.Parse(
                LinkLayer.Ethernet, 
                EthernetTestPacket);
            var tcp_flags = (packet.Payload.Payload as TCPPacket).Flags;
            var output = program.Cycle(packet);
            Assert.Pass();
        }
    }
}