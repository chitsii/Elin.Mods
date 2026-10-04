// Native PC-only element queries need a faction even when CharaGen created a valid NPC.
public sealed class Pr1NativePcContext
{
    public readonly FactionManager Factions = new FactionManager();
    private readonly Chara actor;

    public Pr1NativePcContext(Chara fixture)
    {
        actor = fixture;
        RuntimeAssertions.Require(actor != null && !actor.IsPC, "Native PC context must prepare an owned NPC.");
        var game = EClass.game;
        var originalFactions = game.factions;
        try
        {
            game.factions = Factions;
            Factions.OnCreateGame();
            RuntimeAssertions.Require(Factions.Home != null && Factions.Home.charaElements != null
                && Factions.Home.elements != null && Factions.Home.source != null,
                "Native fixture Home faction/source/element initialization failed.");
            actor.SetFaction(Factions.Home);
            RuntimeAssertions.Require(object.ReferenceEquals(actor.faction, Factions.Home)
                && actor.idFaction == Factions.Home.uid, "Native fixture faction assignment failed.");
        }
        finally { game.factions = originalFactions; }
        // RefreshFaithElement queries IsPCFaction; leave original actors on their original lookup context.
        actor.SetFaith(game.religions.Eyth);
        RuntimeAssertions.Require(object.ReferenceEquals(actor.faith, game.religions.Eyth), "Native fixture faith assignment failed.");
    }

    public Player CreatePlayer()
    {
        // OnCreateGame would create unrelated actors, load PCC files and change Game.config.
        return new Player { chara = actor, uidChara = actor.uid, karma = 30 };
    }

    public void RequireReady(Player player, Thing food)
    {
        RuntimeAssertions.Require(object.ReferenceEquals(EClass.player, player)
            && object.ReferenceEquals(player.chara, actor) && player.uidChara == actor.uid
            && object.ReferenceEquals(EClass.game.factions, Factions)
            && object.ReferenceEquals(actor.faction, Factions.Home)
            && object.ReferenceEquals(Factions.dictAll[actor.idFaction], actor.faction),
            "Native PC faction lookup context is not active.");
        RuntimeAssertions.Require(actor.source != null && actor.race != null && actor.job != null
            && actor.bio != null && actor.elements != null && object.ReferenceEquals(actor.elements.owner, actor)
            && actor.body != null && object.ReferenceEquals(actor.body.owner, actor)
            && actor.renderer != null && object.ReferenceEquals(actor.renderer.owner, actor)
            && actor.conditions != null && actor.faith != null && !actor.isDead,
            "Native food/Vomit/ConSleep actor source, owner or renderer prerequisite missing.");
        RuntimeAssertions.Require(player.stats != null && player.flags != null && player.nums != null
            && player.notices != null && player.queues != null && player.recipes != null
            && player.keyItems != null && player.domains != null && player.dialogFlags != null,
            "Native PC-specific player containers missing.");
        foreach (var condition in actor.conditions)
            RuntimeAssertions.Require(object.ReferenceEquals(condition.owner, actor), "Fixture condition has a foreign owner.");
        RuntimeAssertions.Require(actor.ride == null && actor.parasite == null && actor.master == null
            && actor.homeBranch == null, "Native fixture references a non-fixture mount/master/home branch.");
        RuntimeAssertions.Require(food.source != null && food.elements != null
            && object.ReferenceEquals(food.elements.owner, food) && food.trait.GetType() == typeof(TraitFoodPrepared),
            "Native prepared-food source/element/trait prerequisites missing.");
        // Exercise both absent-element faction fallback and present-element ValueBonus, without effects.
        foreach (int id in new[] { 70, 71, 72, 73, 77, 78, 450, 480, 663, 664, 1200, 1205,
            1234, 1235, 1236, 1250, 1413, 1419, 1650 }) actor.Evalue(id);
        foreach (int id in new[] { 10, 70, 72, 73, 18, 440, 445, 708, 709, 710, 757, 758, 1229 }) food.Evalue(id);
        RuntimeAssertions.Require(!actor.HasElement(1413) && !actor.HasElement(480) && !actor.HasElement(1250),
            "Generated actor has seed-spawning, rotten-edible or machine-food traits outside this fixture contract.");
        RuntimeAssertions.Require(actor.hunger.source != null && actor.stamina.source != null
            && actor.sleepiness.source != null && actor.SAN.source != null,
            "Native hunger/disease/sleep stat source missing.");
    }
}
