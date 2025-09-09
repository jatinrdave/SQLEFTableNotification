using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace SqlDbEntityNotifier.Serializers.Protobuf.Models;

/// <summary>
/// Protobuf representation of a change event.
/// </summary>
public sealed class ChangeEventProto : IMessage<ChangeEventProto>
{
    private static readonly MessageParser<ChangeEventProto> _parser = new(() => new ChangeEventProto());
    public static MessageParser<ChangeEventProto> Parser => _parser;

    public static MessageDescriptor Descriptor => ChangeEventProtoReflection.Descriptor;

    private UnknownFieldSet _unknownFields;
    public UnknownFieldSet UnknownFields
    {
        get => _unknownFields;
        set => _unknownFields = value;
    }

    public ChangeEventProto()
    {
        OnConstruction();
    }

    partial void OnConstruction();

    public ChangeEventProto(ChangeEventProto other) : this()
    {
        source_ = other.source_;
        schema_ = other.schema_;
        table_ = other.table_;
        operation_ = other.operation_;
        timestampUtc_ = other.timestampUtc_ != null ? other.timestampUtc_.Clone() : null;
        offset_ = other.offset_;
        before_ = other.before_;
        after_ = other.after_;
        metadata_ = other.metadata_.Clone();
        _unknownFields = UnknownFieldSet.Clone(other._unknownFields);
    }

    public ChangeEventProto Clone()
    {
        return new ChangeEventProto(this);
    }

    public bool Equals(ChangeEventProto other)
    {
        if (ReferenceEquals(other, null))
        {
            return false;
        }
        if (ReferenceEquals(other, this))
        {
            return true;
        }
        return Source == other.Source
            && Schema == other.Schema
            && Table == other.Table
            && Operation == other.Operation
            && Equals(TimestampUtc, other.TimestampUtc)
            && Offset == other.Offset
            && Before == other.Before
            && After == other.After
            && Metadata.Equals(other.Metadata)
            && Equals(_unknownFields, other._unknownFields);
    }

    public override bool Equals(object other)
    {
        return Equals(other as ChangeEventProto);
    }

    public override int GetHashCode()
    {
        int hash = 1;
        if (Source.Length != 0) hash ^= Source.GetHashCode();
        if (Schema.Length != 0) hash ^= Schema.GetHashCode();
        if (Table.Length != 0) hash ^= Table.GetHashCode();
        if (Operation.Length != 0) hash ^= Operation.GetHashCode();
        if (timestampUtc_ != null) hash ^= TimestampUtc.GetHashCode();
        if (Offset.Length != 0) hash ^= Offset.GetHashCode();
        if (Before.Length != 0) hash ^= Before.GetHashCode();
        if (After.Length != 0) hash ^= After.GetHashCode();
        hash ^= Metadata.GetHashCode();
        if (_unknownFields != null)
        {
            hash ^= _unknownFields.GetHashCode();
        }
        return hash;
    }

    public override string ToString()
    {
        return JsonFormatter.ToDiagnosticString(this);
    }

    public void WriteTo(CodedOutputStream output)
    {
        if (Source.Length != 0)
        {
            output.WriteRawTag(10);
            output.WriteString(Source);
        }
        if (Schema.Length != 0)
        {
            output.WriteRawTag(18);
            output.WriteString(Schema);
        }
        if (Table.Length != 0)
        {
            output.WriteRawTag(26);
            output.WriteString(Table);
        }
        if (Operation.Length != 0)
        {
            output.WriteRawTag(34);
            output.WriteString(Operation);
        }
        if (timestampUtc_ != null)
        {
            output.WriteRawTag(42);
            output.WriteMessage(TimestampUtc);
        }
        if (Offset.Length != 0)
        {
            output.WriteRawTag(50);
            output.WriteString(Offset);
        }
        if (Before.Length != 0)
        {
            output.WriteRawTag(58);
            output.WriteString(Before);
        }
        if (After.Length != 0)
        {
            output.WriteRawTag(66);
            output.WriteString(After);
        }
        metadata_.WriteTo(output, _repeated_metadata_codec);
        if (_unknownFields != null)
        {
            _unknownFields.WriteTo(output);
        }
    }

    public int CalculateSize()
    {
        int size = 0;
        if (Source.Length != 0)
        {
            size += 1 + CodedOutputStream.ComputeStringSize(Source);
        }
        if (Schema.Length != 0)
        {
            size += 1 + CodedOutputStream.ComputeStringSize(Schema);
        }
        if (Table.Length != 0)
        {
            size += 1 + CodedOutputStream.ComputeStringSize(Table);
        }
        if (Operation.Length != 0)
        {
            size += 1 + CodedOutputStream.ComputeStringSize(Operation);
        }
        if (timestampUtc_ != null)
        {
            size += 1 + CodedOutputStream.ComputeMessageSize(TimestampUtc);
        }
        if (Offset.Length != 0)
        {
            size += 1 + CodedOutputStream.ComputeStringSize(Offset);
        }
        if (Before.Length != 0)
        {
            size += 1 + CodedOutputStream.ComputeStringSize(Before);
        }
        if (After.Length != 0)
        {
            size += 1 + CodedOutputStream.ComputeStringSize(After);
        }
        size += metadata_.CalculateSize(_repeated_metadata_codec);
        if (_unknownFields != null)
        {
            size += _unknownFields.CalculateSize();
        }
        return size;
    }

    public void MergeFrom(ChangeEventProto other)
    {
        if (other == null)
        {
            return;
        }
        if (other.Source.Length != 0)
        {
            Source = other.Source;
        }
        if (other.Schema.Length != 0)
        {
            Schema = other.Schema;
        }
        if (other.Table.Length != 0)
        {
            Table = other.Table;
        }
        if (other.Operation.Length != 0)
        {
            Operation = other.Operation;
        }
        if (other.timestampUtc_ != null)
        {
            if (timestampUtc_ == null)
            {
                TimestampUtc = new Timestamp();
            }
            TimestampUtc.MergeFrom(other.TimestampUtc);
        }
        if (other.Offset.Length != 0)
        {
            Offset = other.Offset;
        }
        if (other.Before.Length != 0)
        {
            Before = other.Before;
        }
        if (other.After.Length != 0)
        {
            After = other.After;
        }
        metadata_.Add(other.metadata_);
        if (other._unknownFields != null)
        {
            _unknownFields = UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
        }
    }

    public void MergeFrom(CodedInputStream input)
    {
        uint tag;
        while ((tag = input.ReadTag()) != 0)
        {
            switch (tag)
            {
                default:
                    _unknownFields = UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
                    break;
                case 10:
                    {
                        Source = input.ReadString();
                        break;
                    }
                case 18:
                    {
                        Schema = input.ReadString();
                        break;
                    }
                case 26:
                    {
                        Table = input.ReadString();
                        break;
                    }
                case 34:
                    {
                        Operation = input.ReadString();
                        break;
                    }
                case 42:
                    {
                        if (timestampUtc_ == null)
                        {
                            TimestampUtc = new Timestamp();
                        }
                        input.ReadMessage(TimestampUtc);
                        break;
                    }
                case 50:
                    {
                        Offset = input.ReadString();
                        break;
                    }
                case 58:
                    {
                        Before = input.ReadString();
                        break;
                    }
                case 66:
                    {
                        After = input.ReadString();
                        break;
                    }
                case 74:
                    {
                        metadata_.AddEntriesFrom(input, _repeated_metadata_codec);
                        break;
                    }
            }
        }
    }

    private string source_ = "";
    public string Source
    {
        get => source_;
        set => source_ = ProtoPreconditions.CheckNotNull(value, "value");
    }

    private string schema_ = "";
    public string Schema
    {
        get => schema_;
        set => schema_ = ProtoPreconditions.CheckNotNull(value, "value");
    }

    private string table_ = "";
    public string Table
    {
        get => table_;
        set => table_ = ProtoPreconditions.CheckNotNull(value, "value");
    }

    private string operation_ = "";
    public string Operation
    {
        get => operation_;
        set => operation_ = ProtoPreconditions.CheckNotNull(value, "value");
    }

    private Timestamp timestampUtc_;
    public Timestamp TimestampUtc
    {
        get => timestampUtc_;
        set => timestampUtc_ = value;
    }

    private string offset_ = "";
    public string Offset
    {
        get => offset_;
        set => offset_ = ProtoPreconditions.CheckNotNull(value, "value");
    }

    private string before_ = "";
    public string Before
    {
        get => before_;
        set => before_ = ProtoPreconditions.CheckNotNull(value, "value");
    }

    private string after_ = "";
    public string After
    {
        get => after_;
        set => after_ = ProtoPreconditions.CheckNotNull(value, "value");
    }

    private static readonly FieldCodec<MetadataEntry> _repeated_metadata_codec
        = FieldCodec.ForMessage(74, MetadataEntry.Parser);
    private readonly RepeatedField<MetadataEntry> metadata_ = new RepeatedField<MetadataEntry>();
    public RepeatedField<MetadataEntry> Metadata => metadata_;

    public static class ChangeEventProtoReflection
    {
        public static MessageDescriptor Descriptor { get; } = new MessageDescriptor("ChangeEventProto", new[]
        {
            new FieldDescriptor("source", FieldType.String, 1),
            new FieldDescriptor("schema", FieldType.String, 2),
            new FieldDescriptor("table", FieldType.String, 3),
            new FieldDescriptor("operation", FieldType.String, 4),
            new FieldDescriptor("timestamp_utc", FieldType.Message, 5, typeof(Timestamp)),
            new FieldDescriptor("offset", FieldType.String, 6),
            new FieldDescriptor("before", FieldType.String, 7),
            new FieldDescriptor("after", FieldType.String, 8),
            new FieldDescriptor("metadata", FieldType.Message, 9, typeof(MetadataEntry))
        });
    }
}

/// <summary>
/// Protobuf representation of metadata entry.
/// </summary>
public sealed class MetadataEntry : IMessage<MetadataEntry>
{
    private static readonly MessageParser<MetadataEntry> _parser = new(() => new MetadataEntry());
    public static MessageParser<MetadataEntry> Parser => _parser;

    public static MessageDescriptor Descriptor => MetadataEntryReflection.Descriptor;

    private UnknownFieldSet _unknownFields;
    public UnknownFieldSet UnknownFields
    {
        get => _unknownFields;
        set => _unknownFields = value;
    }

    public MetadataEntry()
    {
        OnConstruction();
    }

    partial void OnConstruction();

    public MetadataEntry(MetadataEntry other) : this()
    {
        key_ = other.key_;
        value_ = other.value_;
        _unknownFields = UnknownFieldSet.Clone(other._unknownFields);
    }

    public MetadataEntry Clone()
    {
        return new MetadataEntry(this);
    }

    public bool Equals(MetadataEntry other)
    {
        if (ReferenceEquals(other, null))
        {
            return false;
        }
        if (ReferenceEquals(other, this))
        {
            return true;
        }
        return Key == other.Key
            && Value == other.Value
            && Equals(_unknownFields, other._unknownFields);
    }

    public override bool Equals(object other)
    {
        return Equals(other as MetadataEntry);
    }

    public override int GetHashCode()
    {
        int hash = 1;
        if (Key.Length != 0) hash ^= Key.GetHashCode();
        if (Value.Length != 0) hash ^= Value.GetHashCode();
        if (_unknownFields != null)
        {
            hash ^= _unknownFields.GetHashCode();
        }
        return hash;
    }

    public override string ToString()
    {
        return JsonFormatter.ToDiagnosticString(this);
    }

    public void WriteTo(CodedOutputStream output)
    {
        if (Key.Length != 0)
        {
            output.WriteRawTag(10);
            output.WriteString(Key);
        }
        if (Value.Length != 0)
        {
            output.WriteRawTag(18);
            output.WriteString(Value);
        }
        if (_unknownFields != null)
        {
            _unknownFields.WriteTo(output);
        }
    }

    public int CalculateSize()
    {
        int size = 0;
        if (Key.Length != 0)
        {
            size += 1 + CodedOutputStream.ComputeStringSize(Key);
        }
        if (Value.Length != 0)
        {
            size += 1 + CodedOutputStream.ComputeStringSize(Value);
        }
        if (_unknownFields != null)
        {
            size += _unknownFields.CalculateSize();
        }
        return size;
    }

    public void MergeFrom(MetadataEntry other)
    {
        if (other == null)
        {
            return;
        }
        if (other.Key.Length != 0)
        {
            Key = other.Key;
        }
        if (other.Value.Length != 0)
        {
            Value = other.Value;
        }
        if (other._unknownFields != null)
        {
            _unknownFields = UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
        }
    }

    public void MergeFrom(CodedInputStream input)
    {
        uint tag;
        while ((tag = input.ReadTag()) != 0)
        {
            switch (tag)
            {
                default:
                    _unknownFields = UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
                    break;
                case 10:
                    {
                        Key = input.ReadString();
                        break;
                    }
                case 18:
                    {
                        Value = input.ReadString();
                        break;
                    }
            }
        }
    }

    private string key_ = "";
    public string Key
    {
        get => key_;
        set => key_ = ProtoPreconditions.CheckNotNull(value, "value");
    }

    private string value_ = "";
    public string Value
    {
        get => value_;
        set => value_ = ProtoPreconditions.CheckNotNull(value, "value");
    }

    public static class MetadataEntryReflection
    {
        public static MessageDescriptor Descriptor { get; } = new MessageDescriptor("MetadataEntry", new[]
        {
            new FieldDescriptor("key", FieldType.String, 1),
            new FieldDescriptor("value", FieldType.String, 2)
        });
    }
}