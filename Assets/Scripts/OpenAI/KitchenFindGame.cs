using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OpenAI;
using OpenAI.Chat;
using UnityEngine;

public class KitchenFindGame
{
    // ── Response type ─────────────────────────────────────────────────────────

    /// <summary>
    /// Parsed result from any OpenAI turn in the Find-Object conversation.
    /// Maps to the JSON shape the model returns: { "lines": [...], "character_last_update": "..." }.
    /// CharacterLastUpdate is only populated by the initial story turn.
    /// </summary>
    public class ClueLinesResponse
    {
        [JsonProperty("lines")]
        public List<string> Lines { get; set; }

        [JsonProperty("character_last_update")]
        public string CharacterLastUpdate { get; set; }
    }

    // ── Data types ────────────────────────────────────────────────────────────

    public class ItemLocation
    {
        public string Section;      // "Fridge" | "Stove" | "Bar" | "Table"
        public string SubSection;   // "Freezer" | "Level-1" … (Fridge only, else "")
        public int    Row;          // 1 or 2
        public int    Position;     // 1-based position within the row
        public List<string> RowItems; // all non-empty display names in the same row
    }

    public class KitchenGameItem
    {
        public string Key;          // KitchenItems.Items key, e.g. "Chicken_Fridge"
        public string DisplayName;  // human-readable, e.g. "Chicken"
        public ItemLocation Location;
    }

    // ── Static kitchen map ────────────────────────────────────────────────────
    // Reflects the layout defined in house_items.txt.
    // Only items that appear in the map can be selected by PickRandomItem().

    private static readonly Dictionary<string, ItemLocation> _map = BuildMap();

    private static Dictionary<string, ItemLocation> BuildMap()
    {
        var m = new Dictionary<string, ItemLocation>();

        void Add(string key, string section, string sub, int row, int pos, List<string> rowItems)
            => m[key] = new ItemLocation
            {
                Section    = section,
                SubSection = sub,
                Row        = row,
                Position   = pos,
                RowItems   = rowItems
            };

        // ── Fridge / Freezer ─────────────────────────────────────────────────
        var frz1 = new List<string> { "Chicken", "Meat Ribs", "Fish", "Whole Ham" };
        var frz2 = new List<string> { "Ice Cream", "Orange Popsicle", "Chocolate Popsicle",
                                      "Strawberry Sundae", "Meat Sausage", "Raw Steak" };

        Add("Chicken_Fridge",            "Fridge", "Freezer", 1, 2, frz1);
        Add("Meatribs_Fridge",           "Fridge", "Freezer", 1, 4, frz1);
        Add("Fish_Fridge",               "Fridge", "Freezer", 1, 5, frz1);
        Add("Whole_Ham_Fridge",          "Fridge", "Freezer", 1, 6, frz1);
        Add("Icecream_Fridge",           "Fridge", "Freezer", 2, 1, frz2);
        Add("Orange_Popsicle_Fridge",    "Fridge", "Freezer", 2, 2, frz2);
        Add("Chocolate_Popsicle_Fridge", "Fridge", "Freezer", 2, 3, frz2);
        Add("Strawberry_Sundae_Fridge",  "Fridge", "Freezer", 2, 4, frz2);
        Add("Meat_Sausage_Fridge",       "Fridge", "Freezer", 2, 5, frz2);
        Add("Raw_Steak_Fridge",          "Fridge", "Freezer", 2, 6, frz2);

        // ── Fridge / Level-1 ─────────────────────────────────────────────────
        var fl1r1 = new List<string> { "Cabbage", "Watermelon", "Pumpkin", "Corn", "Eggplant", "Carrot" };
        var fl1r2 = new List<string> { "Pineapple", "Onion", "Pepper", "Orange", "Mushroom",
                                       "Tomato", "Paprika", "Strawberry", "Pear", "Cherries", "Broccoli" };

        Add("Cabbage_Fridge",    "Fridge", "Level-1", 1,  2, fl1r1);
        Add("Watermelon_Fridge", "Fridge", "Level-1", 1,  5, fl1r1);
        Add("Pumpkin_Fridge",    "Fridge", "Level-1", 1,  7, fl1r1);
        Add("Corn_Fridge",       "Fridge", "Level-1", 1,  9, fl1r1);
        Add("Eggplant_Fridge",   "Fridge", "Level-1", 1, 10, fl1r1);
        Add("Carrot_Fridge",     "Fridge", "Level-1", 1, 11, fl1r1);
        Add("Pineapple_Fridge",  "Fridge", "Level-1", 2,  1, fl1r2);
        Add("Onion_Fridge",      "Fridge", "Level-1", 2,  2, fl1r2);
        Add("Pepper_Fridge",     "Fridge", "Level-1", 2,  3, fl1r2);
        Add("Orange_Fridge",     "Fridge", "Level-1", 2,  4, fl1r2);
        Add("Mushroom_Fridge",   "Fridge", "Level-1", 2,  5, fl1r2);
        Add("Tomato_Fridge",     "Fridge", "Level-1", 2,  6, fl1r2);
        Add("Paprika_Fridge",    "Fridge", "Level-1", 2,  7, fl1r2);
        Add("Strawberry_Fridge", "Fridge", "Level-1", 2,  8, fl1r2);
        Add("Pear_Fridge",       "Fridge", "Level-1", 2,  9, fl1r2);
        Add("Cherries_Fridge",   "Fridge", "Level-1", 2, 10, fl1r2);
        Add("Broccoli_Fridge",   "Fridge", "Level-1", 2, 11, fl1r2);

        // ── Fridge / Level-2 ─────────────────────────────────────────────────
        var fl2r1 = new List<string> { "Salad", "Meat Sandwich", "Pizza" };
        var fl2r2 = new List<string> { "Chinese Food", "Sushi Salmon", "Sushi Egg",
                                       "Meatball & Puree Plate", "Hotdog" };

        Add("Salad_Fridge",                "Fridge", "Level-2", 1, 3, fl2r1);
        Add("Meat_Sandwich_Fridge",        "Fridge", "Level-2", 1, 4, fl2r1);
        Add("Pizza_Fridge",                "Fridge", "Level-2", 1, 6, fl2r1);
        Add("Chinese_Food_Fridge",         "Fridge", "Level-2", 2, 1, fl2r2);
        Add("Sushi_Salmon_Fridge",         "Fridge", "Level-2", 2, 2, fl2r2);
        Add("Sushi_Egg_Fridge",            "Fridge", "Level-2", 2, 3, fl2r2);
        Add("Meatball&Puree_Plate_Fridge", "Fridge", "Level-2", 2, 4, fl2r2);
        Add("Hotdog_Fridge",               "Fridge", "Level-2", 2, 5, fl2r2);

        // ── Fridge / Level-3 ─────────────────────────────────────────────────
        var fl3r1 = new List<string> { "Coke Can", "Soda Cup", "Soda Bottle",
                                       "Red Wine Bottle", "White Wine Bottle" };

        Add("Coke_Can_Fridge",          "Fridge", "Level-3", 1, 1, fl3r1);
        Add("Soda_Cup_Fridge",          "Fridge", "Level-3", 1, 2, fl3r1);
        Add("Soda_Bottle_Fridge",       "Fridge", "Level-3", 1, 3, fl3r1);
        Add("Red_Wine_Bottle_Fridge",   "Fridge", "Level-3", 1, 4, fl3r1);
        Add("White_Wine_Bottle_Fridge", "Fridge", "Level-3", 1, 5, fl3r1);

        // ── Stove top and counter top ─────────────────────────────────────────
        var str1 = new List<string> { "Empty Pot", "Stew Pot", "Oil Bottle",
                                      "Kitchen Microwave", "Coffee Machine" };
        var str2 = new List<string> { "Cooking Knife", "Cooking Fork", "Knife Block",
                                      "Frying Pan", "Toaster" };

        Add("Empty_Pot_Stove",         "Stove", "", 1, 4, str1);
        Add("Stew_Pot_Stove",          "Stove", "", 1, 5, str1);
        Add("Oil_Bottle_Stove",        "Stove", "", 1, 6, str1);
        Add("Kitchen_Microwave_Stove", "Stove", "", 1, 7, str1);
        Add("Coffee_Machine_Stove",    "Stove", "", 1, 8, str1);
        Add("Cooking_Knife_Stove",     "Stove", "", 2, 1, str2);
        Add("Cooking_Fork_Stove",      "Stove", "", 2, 2, str2);
        Add("Knife_Block_Stove",       "Stove", "", 2, 3, str2);
        Add("Frying_Pan_Stove",        "Stove", "", 2, 4, str2);
        Add("Toaster_Stove",           "Stove", "", 2, 8, str2);

        // ── Kitchen bar ───────────────────────────────────────────────────────
        var br1 = new List<string> { "Milk Carton", "Chocolate Milk Carton", "Banana",
                                     "Chocolate Ice Cream Scoop", "Lemonade Soda Cup",
                                     "Pizza", "Mustard Bottle" };
        var br2 = new List<string> { "Kitchen Blender", "Apple", "Strawberry", "Cherries",
                                     "Chocolate Tablet", "Vanilla Ice Cream Scoop",
                                     "Whipped Cream Bar", "Double Cheese Burger", "Cheese Burger",
                                     "Pizza Cutter", "Pizza", "Ketchup Bottle",
                                     "Salad", "Pepper Shaker", "Salt Shaker" };

        Add("Milk_Carton_Bar",              "Bar", "", 1,  2, br1);
        Add("Chocolate_Milk_Carton_Bar",    "Bar", "", 1,  4, br1);
        Add("Banana_Bar",                   "Bar", "", 1,  5, br1);
        Add("Chocolate_IceCream_Scoop_Bar", "Bar", "", 1,  7, br1);
        Add("Lemonad_Soda_Cup_Bar",         "Bar", "", 1,  8, br1);
        Add("Coke_Soda_Bar",                "Bar", "", 1,  9, br1);
        Add("Pizza_Bar",                    "Bar", "", 1, 11, br1);
        Add("Mustard_Bottle_Bar",           "Bar", "", 1, 12, br1);
        Add("Kitchen_Blender_Bar",          "Bar", "", 2,  1, br2);
        Add("Apple_Bar",                    "Bar", "", 2,  2, br2);
        Add("Strawberry_Bar",               "Bar", "", 2,  3, br2);
        Add("Cherries_Bar",                 "Bar", "", 2,  4, br2);
        Add("Chocolate_Tablet_Bar",         "Bar", "", 2,  5, br2);
        Add("Vanilla_IceCream_Scoop_Bar",   "Bar", "", 2,  6, br2);
        Add("Whipped_Cream_Bar",            "Bar", "", 2,  7, br2);
        Add("Double_Cheese_Burger_Bar",     "Bar", "", 2,  8, br2);
        Add("Cheese_Burger_Bar",            "Bar", "", 2,  9, br2);
        Add("Pizza_Cutter_Bar",             "Bar", "", 2, 10, br2);
        Add("Ketchup_Bottle_Bar",           "Bar", "", 2, 12, br2);
        Add("Salad_Bar",                    "Bar", "", 2, 13, br2);
        Add("Pepper_Shaker_Bar",            "Bar", "", 2, 14, br2);
        Add("Salt_Shaker_Bar",              "Bar", "", 2, 15, br2);

        // ── Kitchen table ─────────────────────────────────────────────────────
        var tbr1 = new List<string> { "Croissant", "Tea Mug", "Broth Bowl", "Chopsticks",
                                      "Cake", "Cake Slicer", "Soup Bowl", "Spoon", "Knife",
                                      "Fork", "Paprika Slice", "Avocado Half", "Tomato Slices",
                                      "Bacon", "Donut (Strawberry Sprinkles)", "Donut",
                                      "Donut (Chocolate)", "Coffee Cup" };
        var tbr2 = new List<string> { "Soy", "Cocktail", "Pepper Mill", "Salt Mill",
                                      "Candy Bar", "Bread Loaf", "Cheese Slice",
                                      "Half Pepperoni Log", "Tablet Chocolate", "Cupcake" };

        Add("Croissant_Table",                  "Table", "", 1,  1, tbr1);
        Add("Tea_Mug_Table",                    "Table", "", 1,  2, tbr1);
        Add("Broth_Bowl_Table",                 "Table", "", 1,  3, tbr1);
        Add("Chopsticks_Table",                 "Table", "", 1,  4, tbr1);
        Add("Cake_Table",                       "Table", "", 1,  5, tbr1);
        Add("Cake_Slicer_Table",                "Table", "", 1,  6, tbr1);
        Add("Soup_Bowl_Table",                  "Table", "", 1,  7, tbr1);
        Add("Spoon_Table",                      "Table", "", 1,  8, tbr1);
        Add("Knife_Table",                      "Table", "", 1,  9, tbr1);
        Add("Fork_Table",                       "Table", "", 1, 10, tbr1);
        Add("Paprika_Slice_Table",              "Table", "", 1, 11, tbr1);
        Add("Avocado_Half_Table",               "Table", "", 1, 12, tbr1);
        Add("Tomato_Slices_Table",              "Table", "", 1, 13, tbr1);
        Add("Bacon_Table",                      "Table", "", 1, 14, tbr1);
        Add("Donut_Strawberry_Sprinkles_Table", "Table", "", 1, 15, tbr1);
        Add("Donut_Simple_Table",               "Table", "", 1, 16, tbr1);
        Add("Donut_Chocolate_Table",            "Table", "", 1, 17, tbr1);
        Add("Coffee_Cup_Table",                 "Table", "", 1, 18, tbr1);
        Add("Soy_Table",                        "Table", "", 2,  4, tbr2);
        Add("Cocktail_Table",                   "Table", "", 2,  5, tbr2);
        Add("Pepper_Mill_Table",                "Table", "", 2,  6, tbr2);
        Add("Salt_Mill_Table",                  "Table", "", 2,  7, tbr2);
        Add("Candybar_Table",                   "Table", "", 2,  8, tbr2);
        Add("Bread_Loaf_Table",                 "Table", "", 2, 11, tbr2);
        Add("Cheese_Slice_Table",               "Table", "", 2, 13, tbr2);
        Add("Half_Pepperoni_Log_Table",         "Table", "", 2, 14, tbr2);
        Add("Tablet_Chocolate_Table",           "Table", "", 2, 15, tbr2);
        Add("Cupcake_Table",                    "Table", "", 2, 18, tbr2);

        return m;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Picks a random item from the kitchen map.</summary>
    public KitchenGameItem PickRandomItem()
    {
        var keys = _map.Keys.ToList();
        string key = keys[Random.Range(0, keys.Count)];
        return new KitchenGameItem
        {
            Key         = key,
            DisplayName = KeyToDisplayName(key),
            Location    = _map[key]
        };
    }

    /// <summary>
    /// Looks up the description text an in-scene <c>Interactive</c> component exposes
    /// via <c>Meta["description"]</c> for the given KitchenItems key. Only works once the
    /// Farm scene (with the actual item GameObjects) is loaded — returns empty otherwise.
    /// </summary>
    public string GetItemDescriptionFromScene(string key)
    {
        if (string.IsNullOrEmpty(key)) return string.Empty;
        GameObject go = GameObject.Find(key);
        string desc = go != null ? go.GetComponent<Interactive>()?.GetMeta("description") : null;
        return desc ?? string.Empty;
    }

    /// <summary>Human-readable location string for an item's <see cref="ItemLocation"/> (e.g. "Level-2 shelf of the Fridge, row 1").</summary>
    public static string DescribeLocation(ItemLocation loc)
    {
        string area = string.IsNullOrEmpty(loc.SubSection)
            ? SectionDisplayName(loc.Section)
            : $"{loc.SubSection} shelf of the Fridge";
        return $"{area}, row {loc.Row}";
    }

    /// <summary>Returns the comma-joined list of other item display names sharing the same row, or a fallback phrase.</summary>
    public static string DescribeNeighbors(KitchenGameItem item)
    {
        List<string> others = item.Location.RowItems
            .Where(n => !string.Equals(n, item.DisplayName, System.StringComparison.OrdinalIgnoreCase))
            .ToList();
        return others.Count > 0 ? string.Join(", ", others) : "no other items nearby";
    }

    /// <summary>
    /// Builds the initial system message that seeds the Find-Object conversation.
    /// Only facts we already know are provided — the AI itself invents the character's
    /// reason for wanting the item as part of the story; we never send a "reason" field.
    /// Instructs the model to reply with strict JSON: {"lines":[...],"character_last_update":"..."}.
    /// </summary>
    /// <param name="item">The randomly picked kitchen item.</param>
    /// <param name="character">Optional character profile. Falls back to a generic "Lumiere" persona when null.</param>
    /// <param name="itemDescription">Optional item description pulled from the scene's Interactive.Meta["description"].</param>
    public List<Message> BuildInitialMessages(KitchenGameItem item, CharacterProfile character = null, string itemDescription = null)
    {
        string charName       = !string.IsNullOrEmpty(character?.displayName) ? character.displayName : "Lumiere";
        string charBio        = !string.IsNullOrEmpty(character?.bio)         ? character.bio         : "A friendly resident of the house";
        string charLastUpdate = !string.IsNullOrEmpty(character?.lastUpdate)  ? character.lastUpdate  : "(no previous update)";
        string itemDesc       = !string.IsNullOrEmpty(itemDescription) ? itemDescription : "(no extra description available)";

        string prompt =
$@"You are running a ""Find the Object"" game set inside a house, for a player with aphasia (a language-learning /
speech-therapy exercise). A character who lives in the house needs to find a specific kitchen item and asks the
player to help locate it. The player can see the kitchen and will tap the object they believe is correct.

Character: {charName}
Character description: {charBio}
Character's last known situation: {charLastUpdate}

The item the character is secretly looking for (NEVER say this name to the player): {item.DisplayName}
Where it is: {DescribeLocation(item.Location)}
Other items nearby (same row): {DescribeNeighbors(item)}
What it looks like / extra detail: {itemDesc}

Your job:
1. Invent a SHORT, creative, varied reason {charName} needs this item right now (cooking, cleaning, a memory, a
   craft, anything plausible for a house). Do not reuse the same reason every time — be inventive.
2. Tell this as a short story broken into SEPARATE SHORT LINES (each 1-2 sentences, simple language, easy to
   read on screen one at a time). Across the lines, describe the item's appearance, color, texture, or use —
   WITHOUT ever stating its name directly. You may vaguely hint at where it might be, but do not blatantly give
   the exact position away in the very first lines — leave some challenge for the player.
3. End the final line with an inviting question, e.g. asking the player to go find it.
4. Also write ONE sentence updating {charName}'s current situation/narrative for next time (not shown to the
   player — internal continuity only).

Respond ONLY with this JSON — no extra commentary, no markdown fences:
{{""lines"":[""<line 1>"",""<line 2>"",""...""],""character_last_update"":""<situation update>""}}";

        return new List<Message> { new Message(Role.System, prompt) };
    }

    /// <summary>
    /// Builds the follow-up user turn describing a wrong tap. Appends to the running
    /// conversation rather than starting a new stateless prompt, so the model remembers
    /// the original story, item, and character. Uses only locally-known facts about the
    /// tapped (wrong) item so the model can craft a spatial nudge toward the real target.
    /// </summary>
    /// <param name="targetItem">The correct target item (for context — never revealed by name).</param>
    /// <param name="wrongKey">The KitchenItems key of the object the player actually tapped.</param>
    public Message BuildWrongGuessMessage(KitchenGameItem targetItem, string wrongKey)
    {
        string wrongName = KeyToDisplayName(wrongKey);
        string wrongLocDesc = _map.TryGetValue(wrongKey, out ItemLocation wrongLoc)
            ? DescribeLocation(wrongLoc)
            : "an unknown spot";

        string prompt =
$@"The player just tapped ""{wrongName}"" located at {wrongLocDesc}, which is NOT the item we are looking for.
Stay fully in character and continue the same conversation. Gently and kindly let the player know that is not it
(never sound frustrated or judgmental — people with aphasia need patience and encouragement). Use what you know
about where ""{wrongName}"" is relative to the real target ({DescribeLocation(targetItem.Location)}, near:
{DescribeNeighbors(targetItem)}) to give a soft spatial nudge (e.g. ""closer to..."", ""a bit further from...""),
without stating the target's name. Keep it to 1-3 short lines.

Respond ONLY with this JSON — no extra commentary, no markdown fences:
{{""lines"":[""<line 1>"",""...""]}}";

        return new Message(Role.User, prompt);
    }

    /// <summary>
    /// Builds a follow-up user turn requesting a more specific hint. Escalates specificity
    /// with each call via <paramref name="hintLevel"/> (1 = first hint, 2 = more specific, 3+ = very direct).
    /// </summary>
    public Message BuildHintMessage(KitchenGameItem targetItem, int hintLevel)
    {
        string escalation = hintLevel <= 1
            ? "Give a slightly more specific hint than before — mention the general area of the kitchen more clearly."
            : hintLevel == 2
                ? "Give an even more specific hint — mention one of the neighboring items next to the target as a landmark."
                : "Give a very direct, unmistakable hint — clearly describe exactly where to look, using the neighboring items as landmarks, while still not saying the item's name.";

        string prompt =
$@"The player is stuck and asked for more help finding the item. {escalation}
Remember: exact location is {DescribeLocation(targetItem.Location)}, nearby items: {DescribeNeighbors(targetItem)}.
Stay in character, be warm and encouraging, still never say the item's name directly. Keep it to 1-2 short lines.

Respond ONLY with this JSON — no extra commentary, no markdown fences:
{{""lines"":[""<line 1>"",""...""]}}";

        return new Message(Role.User, prompt);
    }

    /// <summary>Builds a follow-up user turn to celebrate a correct tap.</summary>
    public Message BuildSuccessMessage(KitchenGameItem targetItem)
    {
        string prompt =
$@"The player just correctly found and tapped the item ({targetItem.DisplayName})! Stay in character and react with
short, warm, genuine excitement/gratitude (1-2 short lines). You may now say the item's name since the game is over.

Respond ONLY with this JSON — no extra commentary, no markdown fences:
{{""lines"":[""<line 1>"",""...""]}}";

        return new Message(Role.User, prompt);
    }

    /// <summary>
    /// Parses a raw JSON response (from any turn) into a <see cref="ClueLinesResponse"/>.
    /// Strips markdown code-fences if present. Falls back to a single-line wrapper on parse failure.
    /// </summary>
    public static ClueLinesResponse ParseLinesResponse(string json)
    {
        ClueLinesResponse result = null;
        try
        {
            string cleaned = json.Trim();
            if (cleaned.StartsWith("```"))
            {
                int start = cleaned.IndexOf('{');
                int end   = cleaned.LastIndexOf('}');
                if (start >= 0 && end > start)
                    cleaned = cleaned.Substring(start, end - start + 1);
            }
            result = JsonConvert.DeserializeObject<ClueLinesResponse>(cleaned);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[KitchenFindGame] JSON parse failed: {e.Message}\nRaw: {json}");
        }

        if (result == null || result.Lines == null || result.Lines.Count == 0)
        {
            result = new ClueLinesResponse
            {
                Lines = new List<string> { json },
                CharacterLastUpdate = result?.CharacterLastUpdate ?? string.Empty
            };
        }

        return result;
    }

    /// <summary>Sends the given conversation to OpenAI and returns the raw response string.</summary>
    public async Task<string> SendAsync(List<Message> conversation)
    {
        var openAI = new OpenAIBasics();
        return await openAI.ContinueChatAsync(conversation);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string KeyToDisplayName(string key)
    {
        foreach (string suffix in new[] { "_Fridge", "_Stove", "_Bar", "_Table" })
        {
            if (key.EndsWith(suffix))
            {
                key = key.Substring(0, key.Length - suffix.Length);
                break;
            }
        }
        return key.Replace("_", " ");
    }

    private static string SectionDisplayName(string section)
    {
        switch (section)
        {
            case "Fridge": return "Fridge";
            case "Stove":  return "Stove top and counter top";
            case "Bar":    return "Kitchen bar";
            case "Table":  return "Kitchen table";
            default:       return section;
        }
    }
}
