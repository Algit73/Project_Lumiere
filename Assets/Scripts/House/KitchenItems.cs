using System;
using System.Collections.Generic;
using System.Linq;


public static class KitchenItems
{
    // Static dictionary to store kitchen items
    public static Dictionary<string, string> Items = new Dictionary<string, string>
    {
        { "Chicken_Fridge", "" },
        { "Meatribs_Fridge", "" },
        { "Fish_Fridge", "" },
        { "Whole_Ham_Fridge", "" },
        { "Icecream_Fridge", "" },
        { "Orange_Popsicle_Fridge", "" },
        { "Chocolate_Popsicle_Fridge", "" },
        { "Strawberry_Sundae_Fridge", "" },
        { "Meat_Sausage_Fridge", "" },
        { "Raw_Steak_Fridge", "" },
        { "Cabbage_Fridge", "" },
        { "Watermelon_Fridge", "" },
        { "Pumpkin_Fridge", "" },
        { "Corn_Fridge", "" },
        { "Eggplant_Fridge", "" },
        { "Carrot_Fridge", "" },
        { "Pineapple_Fridge", "" },
        { "Onion_Fridge", "" },
        { "Pepper_Fridge", "" },
        { "Orange_Fridge", "" },
        { "Mushroom_Fridge", "" },
        { "Tomato_Fridge", "" },
        { "Paprika_Fridge", "" },
        { "Strawberry_Fridge", "" },
        { "Pear_Fridge", "" },
        { "Cherries_Fridge", "" },
        { "Broccoli_Fridge", "" },
        { "Salad_Fridge", "" },
        { "Meat_Sandwich_Fridge", "" },
        { "Pizza_Fridge", "" },
        { "Chinese_Food_Fridge", "" },
        { "Sushi_Salmon_Fridge", "" },
        { "Sushi_Egg_Fridge", "" },
        { "Meatball&Puree_Plate_Fridge", "" },
        { "Hotdog_Fridge", "" },
        { "Coke_Can_Fridge", "" },
        { "Soda_Cup_Fridge", "" },
        { "Soda_Bottle_Fridge", "" },
        { "Red_Wine_Bottle_Fridge", "" },
        { "White_Wine_Bottle_Fridge", "" },
        { "Empty_Pot_Stove", "" },
        { "Stew_Pot_Stove", "" },
        { "Oil_Bottle_Stove", "" },
        { "Kitchen_Microwave_Stove", "" },
        { "Coffee_Machine_Stove", "" },
        { "Cooking_Knife_Stove", "" },
        { "Cooking_Fork_Stove", "" },
        { "Knife_Block_Stove", "" },
        { "Frying_Pan_Stove", "" },
        { "KitchenStove_Stove", "" },
        { "Toaster_Stove", "" },
        { "Milk_Carton_Bar", "" },
        { "Chocolate_Milk_Carton_Bar", "" },
        { "Banana_Bar", "" },
        { "Chocolate_IceCream_Scoop_Bar", "" },
        { "Lemonad_Soda_Cup_Bar", "" },
        { "Pizza_Bar", "" },
        { "Musterd_Bottle_Bar", "" },
        { "Kitchen_Blender_Bar", "" },
        { "Apple_Bar", "" },
        { "Strawberry_Bar", "" },
        { "Cherries_Bar", "" },
        { "Chocolate_Tablet_Bar", "" },
        { "Vanilla_IceCream_Scoop_Bar", "" },
        { "Whipped_Cream_Bar", "" },
        { "Double_Cheese_Burger_Bar", "" },
        { "Cheese_Burger_Bar", "" },
        { "Pizza_Cutter_Bar", "" },
        { "Ketchup_Bottle_Bar", "" },
        { "Salad_Bar", "" },
        { "Pepper_Shaker_Bar", "" },
        { "Salt_Shaker_Bar", "" },
        { "Croissant_Table", "" },
        { "Tea_Mug_Table", "" },
        { "Broth_Bowl_Table", "" },
        { "Chopsticks_Table", "" },
        { "Cake_Table", "" },
        { "Cake_Slicer_Table", "" },
        { "Soup_Bowl_Table", "" },
        { "Spoon_Table", "" },
        { "Knife_Table", "" },
        { "Fork_Table", "" },
        { "Paprika_Slice_Table", "" },
        { "Avocado_Half_Table", "" },
        { "Tomato_Slices_Table", "" },
        { "Bacon_Table", "" },
        { "Donut_Strawberry_Sprinkles_Table", "" },
        { "Donut_Simple_Table", "" },
        { "Donut_Chocolate_Table", "" },
        { "Coffee_Cup_Table", "" },
        { "Soy_Table", "" },
        { "Cocktail_Table", "" },
        { "Pepper_Mill_Table", "" },
        { "Salt_Mill_Table", "" },
        { "Candybar_Table", "" },
        { "Bread_Loaf_Table", "" },
        { "Cheese_Slice_Table", "" },
        { "Half_Pepperoni_Log_Table", "" },
        { "Tablet_Chocolate_Table", "" },
        { "Cupcake_Table", "" }
    };

    public static Dictionary<string, string> GetRandomItems(int count)
    {
        // Ensure the count is not larger than the total number of items
        if (count > Items.Count)
        {
            count = Items.Count;
        }

        // Create a random object to select items randomly
        Random random = new Random();
        
        // Get a random subset of the items
        Dictionary<string, string> randomItems = Items
            .OrderBy(x => random.Next()) // Randomize the order of items
            .Take(count) // Take the specified number of items
            .ToDictionary(pair => pair.Key, pair => pair.Value); // Create a new dictionary


        return randomItems;

    }

}

