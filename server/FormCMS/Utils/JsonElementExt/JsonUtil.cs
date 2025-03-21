using System.Text.Json;

namespace FormCMS.Utils.jsonElementExt;

public static class JasonElementExtensions
{
   public static Dictionary<string, object> ToDictionary(this JsonElement element)
   {
      var dict = new Dictionary<string, object>();

      foreach (var prop in element.EnumerateObject())
      {
         dict[prop.Name] = prop.Value.ToPrimitive(); 
      }

      return dict; 
   }

   private static object[] ToArray(this JsonElement jsonElement)
   {
      return jsonElement.EnumerateArray().Select(item => item.ToPrimitive()).ToArray();
   }

   private static object ToPrimitive(this JsonElement element)
   {
         return (element.ValueKind switch
         {
            JsonValueKind.Object => element.ToDictionary(),
            JsonValueKind.Array => element.ToArray(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var i) ? (object)i : element.GetDecimal(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Undefined => null,
            _ => throw new FormatException($"Unexpected JsonValueKind {element.ValueKind}"),
         })!;
   }
}