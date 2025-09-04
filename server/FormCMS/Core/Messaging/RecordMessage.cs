namespace FormCMS.Core.Messaging;

public record RecordMessage(string Operation, string EntityName,  string RecordId, Record Data)
{
    public string Key => $"{EntityName}_{RecordId}";
}