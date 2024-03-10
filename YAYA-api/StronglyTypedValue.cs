using System.Text.Json.Serialization;
using System.Text.Json;
using Google.Protobuf.Collections;

namespace YAYA_api;

public class StronglyTypedValue<T>: IEquatable<StronglyTypedValue<T>> where T: IComparable<T>
{
    public T Value { get; }

    public StronglyTypedValue(T value)
    {
        Value = value;
    }

    public bool Equals(StronglyTypedValue<T>? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return EqualityComparer<T>.Default.Equals(Value, other.Value);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((StronglyTypedValue<T>)obj);
    }

    public override int GetHashCode()
    {
        return EqualityComparer<T>.Default.GetHashCode(Value);
    }

    public static bool operator ==(StronglyTypedValue<T>? left, StronglyTypedValue<T>? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(StronglyTypedValue<T>? left, StronglyTypedValue<T>? right)
    {
        return !Equals(left, right);
    }

    public override string? ToString()
    {
        return Value.ToString();
    }
}

// This should be in a separate file, but I have no time to prepare this project... :(
public class StronglyTypedValueJsonConverter<T> : JsonConverter<T> where T :StronglyTypedValue<Guid> 
{
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {

        try
        {
            var stringValue = reader.GetString();
            if (stringValue is null)
                return null;
            var value = Guid.Parse(stringValue);
            return (T)Activator.CreateInstance(typeToConvert, value)!;
        }
        catch (Exception e)
        {
            /// if guid is not passed, try to parse it as a json object as fallback
            var newOptions = new JsonSerializerOptions(options);
            newOptions.Converters.Remove(this);
            return JsonSerializer.Deserialize<T>(ref reader, newOptions);
        }
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}
