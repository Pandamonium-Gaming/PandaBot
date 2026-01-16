using System.Text.Json.Serialization;
using Postgrest.Attributes;
using Postgrest.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DiscordBot.Models;

[Table("codex")]
public class CodexItem : BaseModel
{
    [PrimaryKey("guid", false)]
    [JsonPropertyName("guid")]
    public string Guid { get; set; } = string.Empty;

    [Column("section")]
    [JsonPropertyName("section")]
    public string Section { get; set; } = string.Empty;

    [Column("data")]
    [JsonPropertyName("data")]
    public CodexItemData? Data { get; set; }
}

public class CodexItemData
{
    [JsonPropertyName("_id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("guid")]
    public string Guid { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("itemName")]
    public string ItemName { get; set; } = string.Empty;

    [JsonPropertyName("grade")]
    public string? Grade { get; set; }

    [JsonPropertyName("level")]
    public int? Level { get; set; }

    [JsonPropertyName("rarityMin")]
    public string? RarityMin { get; set; }

    [JsonPropertyName("rarityMax")]
    public string? RarityMax { get; set; }

    [JsonPropertyName("subType")]
    public string? SubType { get; set; }

    [JsonPropertyName("_summary")]
    public string? Summary { get; set; }

    [JsonPropertyName("displayIcon")]
    [Newtonsoft.Json.JsonConverter(typeof(DisplayIconConverter))]
    public string? DisplayIcon { get; set; }  // TODO: Map to ashescodex.com URLs in the future

    [JsonPropertyName("gameplayTags")]
    public GameplayTags? Tags { get; set; }

    [JsonPropertyName("_defaultRecipe")]
    public bool? IsRecipe { get; set; }

    [JsonPropertyName("_learnableRecipes")]
    public List<LearnableRecipe>? LearnableRecipes { get; set; }

    [JsonPropertyName("statBlock")]
    public object? StatBlock { get; set; }

    // Creature-specific fields
    [JsonPropertyName("locationName")]
    public string? LocationName { get; set; }

    [JsonPropertyName("locations")]
    public List<string>? Locations { get; set; }

    [JsonPropertyName("spawnLocations")]
    public object? SpawnLocations { get; set; }

    // Item drop fields
    [JsonPropertyName("_droppedIn")]
    public object? DroppedIn { get; set; }
    
    [JsonPropertyName("droppedBy")]
    public List<string>? DroppedBy { get; set; }

    [JsonPropertyName("dropSources")]
    public object? DropSources { get; set; }

    [JsonPropertyName("lootTable")]
    public object? LootTable { get; set; }
}

// TODO: ItemDisplayIcon - Future enhancement to map icon paths to ashescodex.com URLs
// The displayIcon field contains paths like "/Game/UI/Icons/Items/..."
// These can be mapped to https://ashescodex.com/... when icon support is added

public class GameplayTags
{
    [JsonPropertyName("gameplayTags")]
    public List<TagItem>? Tags { get; set; }

    [JsonPropertyName("parentTags")]
    public List<TagItem>? ParentTags { get; set; }
}

public class TagItem
{
    [JsonPropertyName("tagName")]
    public string? TagName { get; set; }
}

// Custom converter to handle displayIcon being either a string or an object
public class DisplayIconConverter : Newtonsoft.Json.JsonConverter<string?>
{
    public override string? ReadJson(JsonReader reader, Type objectType, string? existingValue, bool hasExistingValue, Newtonsoft.Json.JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;
        
        if (reader.TokenType == JsonToken.String)
            return reader.Value?.ToString();
        
        // If it's an object, extract the assetPathName or return null
        if (reader.TokenType == JsonToken.StartObject)
        {
            var obj = JObject.Load(reader);
            return obj["assetPathName"]?.ToString() ?? obj.ToString();
        }
        
        return null;
    }

    public override void WriteJson(JsonWriter writer, string? value, Newtonsoft.Json.JsonSerializer serializer)
    {
        writer.WriteValue(value);
    }
}
