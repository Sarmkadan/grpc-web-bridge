using BenchmarkDotNet.Attributes;
using GrpcWebBridge.Domain.Exceptions;
using GrpcWebBridge.Domain;
using System;
using System.Collections.Generic;

namespace GrpcWebBridge.Benchmarks
{
    [MemoryDiagnoser]
    public class GrpcWebBridgeExceptionBenchmarks
    {
        private string _message = default!;
        private Exception _innerException = default!;
        private string _errorCode = default!;
        private GrpcStatusCode _grpcStatusCode;
        private string[] _contextKeys = default!;
        private object[] _contextValues = default!;

        [Params(10, 100, 1000)]
        public int ContextSize;

        [GlobalSetup]
        public void Setup()
        {
            _message = "Test error message";
            _innerException = new InvalidOperationException("inner");
            _errorCode = "ERR001";
            _grpcStatusCode = GrpcStatusCode.Internal;
            // Generate enough keys/values for the largest param size (1000)
            int maxSize = 1000;
            _contextKeys = new string[maxSize];
            _contextValues = new object[maxSize];
            for (int i = 0; i < maxSize; i++)
            {
                _contextKeys[i] = $"Key{i}";
                _contextValues[i] = $"Value{i}";
            }
        }

        [Benchmark]
        public GrpcWebBridgeException Constructor_WithMessage()
        {
            return new GrpcWebBridgeException(_message);
        }

        [Benchmark]
        public GrpcWebBridgeException Constructor_WithMessageAndInner()
        {
            return new GrpcWebBridgeException(_message, _innerException);
        }

        [Benchmark]
        public GrpcWebBridgeException Constructor_WithMessageAndErrorCode()
        {
            return new GrpcWebBridgeException(_message, _errorCode);
        }

        [Benchmark]
        public GrpcWebBridgeException Constructor_WithMessageAndGrpcStatus()
        {
            return new GrpcWebBridgeException(_message, _grpcStatusCode);
        }

        [Benchmark]
        public void AddContext_Items()
        {
            var ex = new GrpcWebBridgeException(_message);
            for (int i = 0; i < ContextSize; i++)
            {
                ex.AddContext(_contextKeys[i], _contextValues[i]);
            }
        }

        [Benchmark]
        public void GetContext_Items()
        {
            var ex = new GrpcWebBridgeException(_message);
            for (int i = 0; i < ContextSize; i++)
            {
                ex.AddContext(_contextKeys[i], _contextValues[i]);
            }
            for (int i = 0; i < ContextSize; i++)
            {
                var value = ex.GetContext(_contextKeys[i]);
            }
        }

        [Benchmark]
        public string ToString_WithAllData()
        {
            var ex = new GrpcWebBridgeException(_message, _errorCode);
            ex.GrpcStatus = _grpcStatusCode;
            for (int i = 0; i < 10; i++)
            {
                ex.AddContext(_contextKeys[i], _contextValues[i]);
            }
            return ex.ToString();
        }

        [Benchmark]
        public GrpcWebBridgeException WithContext_Chained()
        {
            var ex = new GrpcWebBridgeException(_message);
            for (int i = 0; i < 10; i++)
            {
                ex.WithContext(_contextKeys[i], _contextValues[i]);
            }
            return ex;
        }

        [Benchmark]
        public GrpcWebBridgeException WithInnerException_Chained()
        {
            var ex = new GrpcWebBridgeException(_message);
            for (int i = 0; i < 10; i++)
            {
                ex.WithInnerException(new InvalidOperationException($"inner {i}"));
            }
            return ex;
        }
    }
}