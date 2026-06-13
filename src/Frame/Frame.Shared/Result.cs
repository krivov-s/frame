
namespace Frame.Shared
{
    public abstract class Result
    {
        protected bool _isError;
        public Exception? Exception { get; protected set; }
        public string ErrorResult { get; private set; }
        public bool IsError => _isError; 
        protected Result()
        {
            _isError = false;
            ErrorResult = "";
            Exception = null;
        }
        protected Result(string errorResult, bool isError, Exception? exception = null)
        {
            _isError = isError;
            ErrorResult = errorResult;
            Exception = exception;
        }
        protected Result(string errorResult, Exception exception)
        {
            _isError = true;
            ErrorResult = errorResult;
            Exception = exception;
        }

        private sealed class TSuccess(string message = "") : Result(message, false);
        private sealed class TError(string errorResult, Exception? exception) : Result(errorResult, true, exception);

        public static Result Success { get => new TSuccess(); }
        public static Result SuccessWithMessage(string message) => new TSuccess(message);
        public static Result Error(Result err) { return new TError(err.ErrorResult, err.Exception); }
        public static Result Error(string errorResult, Exception? exception = null)
        {
            if (string.IsNullOrEmpty(errorResult))
            {
                throw new ArgumentException("При формировании Result.Error errorResult не должен быть пустым!");
            }
            return new TError(errorResult, exception);
        }

        public override string ToString()
        {
            string strDisplay = IsError ? "Ошибка" : "Успешно";


            if(ErrorResult.Length > 0)
            {
                strDisplay += (": " + ErrorResult);
            }

            return strDisplay;
        }

        public virtual void CheckAndThrow(string message)
        { 
            if(IsError)
            {
                string strError = $"{message} : {ErrorResult}";
                throw new FrameException(strError);
            }
        }
    }

    public class Result<T> : Result
    {
        public T? Value { get; private set; }
        private Result(T? value, string errorResult, bool isError, Exception? exception = null) : base(errorResult, isError, exception)
        {
            Value = value;
        }

        public new static Result<T> Success(T? value, string message = "") => new(value, message, false);
        public static Result<T> SuccessWithMessage(T? value, string message = "") => new(value, message, false);

        public new static Result<T> Error(string errorResult, Exception? exception = null)
        {
            return Error(errorResult, default, exception);
        }

        public static Result<T> Error(string errorResult, T? value, Exception? exception = null)
        {
            if (errorResult.Length == 0)
            {
                throw new ArgumentException("При формировании Result.Error errorResult не должен быть пустым!");
            }
            return new Result<T>(value, errorResult, true, exception);
        }
        public new static Result<T> Error(Result err) 
        { 
            return new Result<T>(default, err.ErrorResult, true, err.Exception); 
        }

        public override void CheckAndThrow(string message)
        {
            base.CheckAndThrow(message);
            if (Equals(Value, default(T)))
            {
                string strError = $"{message} : ошибки нет, а значение пустое (Result.Value == default(T))!";
                throw new FrameException(strError);
            }
        }
        
        public bool IsErrorOrNull => _isError || Equals(Value, default(T));

        public static implicit operator T?(Result<T> result) => result.Value;
    }
}
