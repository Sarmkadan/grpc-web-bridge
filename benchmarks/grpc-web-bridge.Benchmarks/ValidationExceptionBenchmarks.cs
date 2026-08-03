using BenchmarkDotNet.Attributes;
using GrpcWebBridge.Domain.Exceptions;
using System;

namespace GrpcWebBridge.Benchmarks
{
    [MemoryDiagnoser]
    public class ValidationExceptionBenchmarks
    {
        private string _message = null!;
        private string _fieldName = null!;
        private object? _invalidValue;
        private string _validationRule = null!;
        private Exception _innerException = null!;
        private ValidationException _exceptionWithAllParams = null!;

        [Params(10, 100, 1000)]
        public int MessageLength;

        [GlobalSetup]
        public void Setup()
        {
            _message = new string('x', MessageLength);
            _fieldName = "FieldName";
            _invalidValue = 42;
            _validationRule = "Required";
            _innerException = new InvalidOperationException("inner exception");

            _exceptionWithAllParams = new ValidationException(_fieldName, _invalidValue, _validationRule, _message);
        }

        [Benchmark]
        public ValidationException DefaultConstructor() => new ValidationException();

        [Benchmark]
        public ValidationException ConstructorWithMessage() => new ValidationException(_message);

        [Benchmark]
        public ValidationException ConstructorWithMessageAndInner() => new ValidationException(_message, _innerException);

        [Benchmark]
        public ValidationException ConstructorWithAllParams() => new ValidationException(_fieldName, _invalidValue, _validationRule, _message);

        [Benchmark]
        public string ToStringMethod() => _exceptionWithAllParams.ToString();
    }
}