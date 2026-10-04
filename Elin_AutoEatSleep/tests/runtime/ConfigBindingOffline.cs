public static class Pr1ConfigBindingOffline
{
    public static int Main(string[] args)
    {
        string root = System.IO.Path.Combine(args[0], "config-" + System.Guid.NewGuid().ToString("N"));
        string freshPath = System.IO.Path.Combine(root, "fresh.cfg");
        string savedPath = System.IO.Path.Combine(root, "explicit.cfg");
        try
        {
            var freshFile = new BepInEx.Configuration.ConfigFile(freshPath, false) { SaveOnConfigSet = false };
            var fresh = new Elin_AutoEatSleep.ModConfig(freshFile);
            if (fresh.HungerThreshold.Value != 3) throw new System.Exception("fresh default: expected 3, got " + fresh.HungerThreshold.Value);
            System.IO.Directory.CreateDirectory(root);
            var savedFile = new BepInEx.Configuration.ConfigFile(savedPath, false) { SaveOnConfigSet = false };
            savedFile.Bind("AutoEat", "HungerThreshold", 3).Value = 1;
            savedFile.Save();
            string before = System.Convert.ToBase64String(System.IO.File.ReadAllBytes(savedPath));
            var reloaded = new BepInEx.Configuration.ConfigFile(savedPath, false) { SaveOnConfigSet = false };
            var existing = new Elin_AutoEatSleep.ModConfig(reloaded);
            if (existing.HungerThreshold.Value != 1) throw new System.Exception("existing explicit value was replaced");
            if (before != System.Convert.ToBase64String(System.IO.File.ReadAllBytes(savedPath))) throw new System.Exception("existing config file changed");
            System.Console.WriteLine("Real BepInEx ConfigFile: fresh=3, existing=1, saved bytes unchanged.");
            return 0;
        }
        catch (System.Exception ex) { System.Console.WriteLine("FAIL: " + ex.Message); return 1; }
        finally
        {
            if (System.IO.File.Exists(freshPath)) System.IO.File.Delete(freshPath);
            if (System.IO.File.Exists(savedPath)) System.IO.File.Delete(savedPath);
            if (System.IO.Directory.Exists(root)) System.IO.Directory.Delete(root, false);
        }
    }
}
