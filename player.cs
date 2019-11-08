using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
/**
 * This was created for
 * Hypersonic (Bomberman clone) challenge (www.codingame.com), with nickname Balthar
 * 
 * 
-------------------------------------------------------------------------------------------
     Huge thanks to http://blog.two-cats.com/2014/06/a-star-example/ aka. Two-Cats Blog for the awesome A* Pathfinding
--------------------------------------------------------------------------------------------------------

ps. There are propably ton of variables that are pretty much unused by now. 
*/

public static class Debugging
{
    public static bool SHOW_PATHFINDER_ROUTE = false;
    public static bool SHOW_CALCULATION_PARAMS = false;
    public static bool SHOW_OBJECTS_BLAST_RADIUS = false;
    public static bool SHOW_DEBUG_INFO = true;
    public static bool SHOW_DEBUG_ADVANCED = false;
    public static bool CALCULATE_ELAPSED_TIME = false;
    public static bool IN_VISUAL_STUDIO = false;
    public static bool SHOW_PATHFINDER_ROUTE_SEARCH = false;
}
public static class DebugInputs
{
    public static string Entities = "0 0 0 0 1 3";
    public static int EntityCount = 1;
    public static string MapString = @"..1.......1..
.X.X.X.X.X.X.
00....2....00
.X.X0X1X0X.X.
22.211.112.22
.X.X0X.X0X.X.
22.211.112.22
.X.X0X1X0X.X.
00....2....00
.X.X.X.X.X.X.
..1.......1..";
    public static string InitializationString = @"13 11 0";
}
public static class GameSettings
{
    public const int PATHFINDER_HOTZONE_SUBSTRACTED_FROM = 8;
    public const float PATHFINDER_HOTZONE_MODIFIER = 0.35f; // (PATHFINDER_HOTZONE_SUBSTRACTED_FROM - Bombtimer )this * bomb timer, added to path's points should keep bombed areas out of path. 
    public const int SEARCH_RADIUS = 12; //Obselete
    public const int WHEN_HOT_ZONE_BECOMES_HOT = 3; // used in calculation if route is safe
    public const int HOTZONE_SAFETY_LIMIT = 3; // When tile is NO-GO
    public const int DISTANCE_BASE_VALUE = 48; //Increasing this will value closer objectives more. 
    public const int DISTANCE_MODIFIER = 2; //Modifier to Distance Base value substract mutiplies distance value
    public const int POINTS_FROM_SOMETHING_TO_BREAK = 6; // Increasing this will make selected objective more likely to be a box destroying
    public const int POINTS_FROM_ITEM = 8;// Increasing this will make selected objective more likely to be a iten gathering
    public const int POINTS_FROM_SAFETY = 8; // Increasing this will make AI value more safety over objective
    public const int REQUIRED_POINTS_FOR_MOVEMENT = 2; //OBSELETE
    public const int POINTS_FROM_ENEMY_PLAYER = 0; // Increasing this will make selected objective be more likely a killign enemy player
    public const int DISTANCE_WHEN_TO_PLANT_ON_ENEMY = 3; // What is minimum distance when to plant a bomb when targeting enemy player
    public const int STUCK_LIMIT = 3;

    public const int ROUTE_START_INDEX = 0;
}
public class HotZone
{
    public Coordinate Location { get; set; } = new Coordinate();
    public int RoundsTillDeathly { get; set; } = 0;
}

public static class GameObjects
{
    public static int MapWidth { get; set; }
    public static int MapHeight { get; set; }
    public static bool Enrage { get; set; } = false;
    public static Map map { get; set; }
    public static int MyID { get; set; }
    public static List<PlayerPoints> Points { get; set; } = new List<PlayerPoints>();
    public static PlayerEntity EnemyPlayer
    {
        get
        {

            return map.Players.Where(p => p.PlayerID != MyID).FirstOrDefault();
        }
    }
    public static Coordinate TargetCoordinates { get; set; } = new Coordinate();
    public static Coordinate PreviusLocation { get; set; }
    public static List<Coordinate> Route { get; set; } = new List<Coordinate>();
    public static int RouteIndex = 0;
    public static PlayerEntity MyPlayer
    {
        get
        {
            return map.Players.Where(p => p.PlayerID == MyID).FirstOrDefault();
        }
    }

}
public static class HelperMethods
{
    public static int ClearedInt(int input, int min, int max)
    {
        int rValue = 0;
        if (input <= min) rValue = min;
        else if (input >= max) rValue = max;
        else rValue = input;
        return rValue;
    }
    public static void Error(string text)
    {
        if (Debugging.SHOW_DEBUG_INFO)
            Console.Error.WriteLine(text);
    }
    public static int DistanceBetweenTwoPoints(Coordinate point1, Coordinate point2)
    {
        // xd = x2-x1 yd = y2-y1 Distance = sqrt(xd * xd + yd * yd)
        int distance = (int)(Math.Pow(point2.XCoordinate - point1.XCoordinate, 2) + Math.Pow(point2.YCoordinate - point1.YCoordinate, 2));
        return distance;
    }
}
class Player
{
    static void InitializeOneTime()
    {
        string[] inputs;
        string inputText;
        if (Debugging.IN_VISUAL_STUDIO)
        {
            inputText = DebugInputs.InitializationString;
        }
        else
        {
            inputText = Console.ReadLine();
        }

        inputs = inputText.Split(' ');
        int width = int.Parse(inputs[0]);
        int height = int.Parse(inputs[1]);
        int myId = int.Parse(inputs[2]);
        //HelperMethods.Error(inputText);
        GameObjects.MyID = myId;
        GameObjects.MapHeight = height;
        GameObjects.MapWidth = width;
    }
    static void LoadEveryLoop()
    {
        GameObjects.map = LoadMap();
        LoadEntities();
        UpdateBombTimers();
        LoadHotZones();
        CalculatePoints();

    }
    public static bool FirsTime = true;

    public static void LoadEntities()
    {
        string[] inputs;
        GameObjects.map.HotZones.Clear();
        int entities;
        if (Debugging.IN_VISUAL_STUDIO)
        {
            entities = DebugInputs.EntityCount;
        }
        else
        {
            entities = int.Parse(Console.ReadLine());
        }

        for (int i = 0; i < entities; i++)
        {
            string inputString;
            if (Debugging.IN_VISUAL_STUDIO)
            {
                inputString = DebugInputs.Entities;
            }
            else
            {
                inputString = Console.ReadLine();
            }

            //HelperMethods.Error(inputString);
            inputs = inputString.Split(' ');
            int entityType = int.Parse(inputs[0]);
            int owner = int.Parse(inputs[1]);
            int x = int.Parse(inputs[2]);
            int y = int.Parse(inputs[3]);
            int param1 = int.Parse(inputs[4]);
            int param2 = int.Parse(inputs[5]);
            EntityType type = (EntityType)entityType;
            switch (type)
            {
                case EntityType.Bombs:
                    BombEntity bomb = new BombEntity();
                    bomb.Location = new global::Coordinate(x, y);
                    bomb.NumberOfRoundsTillExplosion = param1;
                    bomb.ExplosionRange = param2;
                    bomb.PlayerID = owner;
                    GameObjects.map.Bombs.Add(bomb);
                    break;
                case EntityType.Players:
                    PlayerEntity playah = new PlayerEntity();
                    playah.Location = new global::Coordinate(x, y);
                    playah.NumberOfBombsLeft = param1;
                    playah.RangeOfBombs = param2;
                    playah.PlayerID = owner;
                    if (FirsTime)
                    {
                        PlayerPoints pp = new PlayerPoints();
                        pp.playerid = owner;
                        pp.aproxpoints = 0;
                        GameObjects.Points.Add(pp);
                    }
                    GameObjects.map.Players.Add(playah);
                    break;
                case EntityType.Items:
                    ItemEntity item = new ItemEntity();
                    item.Location = new Coordinate(x, y);
                    item.itemType = (ItemType)param1;
                    GameObjects.map.Items.Add(item);
                    break;
                default:
                    HelperMethods.Error("Unknown entity, ignoring!");
                    break;
            }

        }
        FirsTime = false;


    }

    public static void LoadHotZones()
    {
        foreach (BombEntity b in GameObjects.map.Bombs)
        {
            //if (b.NumberOfRoundsTillExplosion <= GameSettings.WHEN_HOT_ZONE_BECOMES_HOT)
            //{
            foreach (MapTile t in b.TilesInBlastRadius())
            {
                if (GameObjects.map.HotZones.Where(h => h.Location.Equals(t.Location)).ToList().Count == 0)
                {
                    var hotZone = new HotZone();
                    hotZone.Location = t.Location;
                    hotZone.RoundsTillDeathly = b.NumberOfRoundsTillExplosion;
                    GameObjects.map.HotZones.Add(hotZone);
                }
                //}
            }
        }
        HelperMethods.Error("There are " + GameObjects.map.HotZones.Count + " hot zones in this map currently!");
    }
    public static void CalculatePoints()
    {
        HelperMethods.Error("Calculating player points");
        foreach (BombEntity b in GameObjects.map.Bombs.Where(bo => bo.NumberOfRoundsTillExplosion == 1).ToList())
        {
            int points = AmountOfEntities(b.Location, ItemAtLocation.box);


            HelperMethods.Error("Player: " + b.PlayerID + " is going get " + points + " points");
            GameObjects.Points.Where(pla => pla.playerid == b.PlayerID).FirstOrDefault().aproxpoints += points;



        }

    }
    public static Map LoadMap()
    {
        Map thisMap = new Map();
        try
        {
            int height = GameObjects.MapHeight;
            int width = GameObjects.MapWidth;
            List<MapTile> Tiles = new List<MapTile>();
            string[] Inputs = new string[0];
            if (Debugging.IN_VISUAL_STUDIO)
            {
                Inputs = DebugInputs.MapString.Split('\n');
            }
            for (int i = 0; i < height; i++)
            {
                string row;
                if (Debugging.IN_VISUAL_STUDIO)
                {
                    row = Inputs[i].Replace("\r", String.Empty);//DebugInputs.MapString.Substring(i * width, width);
                }
                else
                {
                    row = Console.ReadLine();
                }

                //HelperMethods.Error(row);
                for (int x = 0; x < width; x++)
                {
                    MapTile t = new MapTile();
                    Coordinate Location = new Coordinate(x, i);
                    t.Location = Location;
                    t.Type = TileType.floor;
                    switch (row[x])
                    {
                        case '.':
                            break;
                        case '0':
                            BoxEntity box = new BoxEntity();
                            box.Location = t.Location;
                            box.ContainsItem = ItemType.Empty;
                            thisMap.Boxes.Add(box);
                            break;
                        case 'X':
                            t.Type = TileType.wall;
                            break;
                        default:
                            int val = int.Parse(row[x].ToString());
                            switch (val)
                            {
                                case 1:
                                    BoxEntity box2 = new BoxEntity();
                                    box2.Location = t.Location;
                                    box2.ContainsItem = (ItemType)1;
                                    thisMap.Boxes.Add(box2);
                                    break;
                                case 2:
                                    BoxEntity box3 = new BoxEntity();
                                    box3.Location = t.Location;
                                    box3.ContainsItem = (ItemType)2;
                                    thisMap.Boxes.Add(box3);
                                    break;
                            }
                            break;
                    }
                    Tiles.Add(t);
                }
            }
            thisMap.height = height;
            thisMap.tiles = Tiles;
            thisMap.width = width;

        }
        catch (Exception ex)
        {
            HelperMethods.Error("Errors in LoadMap function: " + ex.Message);
        }
        return thisMap;
    }
    public static Coordinate LastLocation = new Coordinate();
    public static long TopTime = 0;
    public static System.Diagnostics.Stopwatch watch = null;
    public static string message = "";
    static void Main(string[] args)
    {
        InitializeOneTime();
        int stuckCounter = 0;
        while (true)
        {
            if (Debugging.CALCULATE_ELAPSED_TIME)
            {
                if (watch == null)
                {
                    watch = System.Diagnostics.Stopwatch.StartNew();
                }
                else
                {
                    watch.Reset();
                }

            }
            LoadEveryLoop();
            HelperMethods.Error("I am on lead is: " + GameObjects.MyPlayer.InLead.ToString());
            if (Debugging.CALCULATE_ELAPSED_TIME) watch.Start();
            string commandText = "";
            if (GameObjects.MyPlayer.Location.Equals(LastLocation))
            {
                stuckCounter++;
            }
            else
            {
                stuckCounter = 0;
            }

            bool currentLocationSafe = IsSafeSpot(GameObjects.MyPlayer.Location, false);
            commandText = "MOVE";

            if (currentLocationSafe) // we are safe, for now
            {
                HelperMethods.Error("- CurrentLocation is Safe");
                if (RouteFinished())
                {
                    HelperMethods.Error("- Route is finished");
                    if (ShouldPlant())
                    {
                        HelperMethods.Error("- Player should plant, and can plant");
                        if (IsSafeForPlant())
                        {
                            HelperMethods.Error("- Current location is safe for plant and i have escape route. ");
                            commandText = "BOMB";
                        }

                        else
                        {
                            HelperMethods.Error("- CurrentLocation is NOT safe for plant because i dindt find escape route");
                            HelperMethods.Error("- FindNewTarget is called");
                            FindNewTarget();
                        }
                    }
                    else
                    {
                        HelperMethods.Error("- Player should not plant, because there are no objectives to destroy or player just cant plant");
                        HelperMethods.Error("- FindNewTarget is called");
                        FindNewTarget();
                    }
                }
                else
                {

                    HelperMethods.Error("- Route is not finished");
                    if (IsRouteStillSafe())
                    {

                        //HelperMethods.Error("- But its still safe");
                        //if (ShouldPlant())
                        //{
                        //    HelperMethods.Error("- Player should plant, and can plant");
                        //    if (IsSafeForPlant())
                        //    {
                        //        HelperMethods.Error("- Current location is safe for plant and i have escape route. ");
                        //        commandText = "BOMB";
                        //    }

                        //    else
                        //    {
                        //        HelperMethods.Error("- CurrentLocation is NOT safe for plant because i dindt find escape route");
                        //    }
                        //}
                        //else
                        //{

                        //}
                    }
                    else
                    {

                        HelperMethods.Error("- Route doesnt seem to be safe anymore, finding new place.");
                        HelperMethods.Error("- FindNewTarget is called");
                        FindNewTarget();
                    }
                }
            }
            else
            {
                if (!IsRouteDestinationStillSafe())
                {
                    HelperMethods.Error("- Current position is not safe and destination is not safe");
                    HelperMethods.Error("- FindNewTarget is called");
                    FindNewTarget();
                }
                else
                {
                    HelperMethods.Error("- Current position not safe and destination is not safe");
                    HelperMethods.Error("- But destination is safe so we keep going ther");
                }


            }
            if (!currentLocationSafe && stuckCounter >= GameSettings.STUCK_LIMIT)
            {
                HelperMethods.Error("I seem to be stuck here? Is that true? i've been here now :" + stuckCounter + " iterations");
                //FindNewTarget();
                stuckCounter = 0;
            }
            if (GameObjects.Route.Count > 0)
            {
                HelperMethods.Error("My curren target destination is :" + GameObjects.Route[GameObjects.Route.Count - 1].ToString());
            }

            LastLocation = GameObjects.TargetCoordinates;
            commandText += " " + GetNextCoord();

            if (Debugging.CALCULATE_ELAPSED_TIME)
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                if (TopTime < elapsedMs) TopTime = elapsedMs;
                Console.Error.WriteLine("Exec: " + elapsedMs.ToString() + "ms, Highest: " + TopTime.ToString() + "ms");
            }
            commandText += " " + message;
            message = "";
            Console.WriteLine(commandText);
        }
    }
    public static void UpdateBombTimers()
    {

        try
        {
            int iterationCounter = 0;
            HelperMethods.Error("--Updating bomb chainreaction timers.");
            foreach (BombEntity b in GameObjects.map.Bombs)
            {
                foreach (MapTile t in b.TilesInBlastRadius())
                {
                    var bombs = GameObjects.map.Bombs.Where(bo => bo.Location.Equals(t.Location)).FirstOrDefault();
                    iterationCounter++;
                    if (bombs != null)
                    {
                        if (bombs.NumberOfRoundsTillExplosion < b.NumberOfRoundsTillExplosion)
                        {
                            HelperMethods.Error("--- Updating bomb in loc: " + b.Location.ToString() + " timer from:");
                            HelperMethods.Error(b.NumberOfRoundsTillExplosion + " -> " + bombs.NumberOfRoundsTillExplosion + " because bomb in location: " + bombs.Location.ToString() + " will detonate it.");
                            b.NumberOfRoundsTillExplosion = bombs.NumberOfRoundsTillExplosion;
                        }
                        else if (bombs.NumberOfRoundsTillExplosion > b.NumberOfRoundsTillExplosion)
                        {
                            HelperMethods.Error("--- Updating bomb in loc: " + bombs.Location.ToString() + " timer from:");
                            HelperMethods.Error(bombs.NumberOfRoundsTillExplosion + " -> " + b.NumberOfRoundsTillExplosion + " because bomb in location: " + b.Location.ToString() + " will detonate it.");
                            bombs.NumberOfRoundsTillExplosion = b.NumberOfRoundsTillExplosion;
                        }
                    }
                }
            }
            HelperMethods.Error("--Update is complete. took function: " + iterationCounter + " loops to update bombs");
        }
        catch (Exception ex)
        {
            HelperMethods.Error("UpdateBombTimers error: " + ex.Message);
        }
    }

    public static bool IsRouteStillSafe()
    {
        bool val = true;
        int currentStep = 0;
        for (int i = GameObjects.RouteIndex; i < GameObjects.Route.Count - 1; i++)
        {
            var hz = GameObjects.map.HotZones.Where(hot => hot.Location.Equals(GameObjects.Route[i])).FirstOrDefault();
            currentStep = GameObjects.RouteIndex + i;
            if (hz != null)
            {
                if (hz.RoundsTillDeathly <= currentStep)
                {
                    val = false;
                }
            }

        }
        return val;
    }
    public static bool IsThisRouteSafe(List<Coordinate> Route)
    {
        //if (Debugging.SHOW_DEBUG_INFO)
        //{
        //    int i = 0;
        //    foreach (Coordinate c in Route)
        //    {
        //        HelperMethods.Error("Index: " + i + " has coordinate of " + c.ToString());
        //    }

        //}
        bool val = true;
        int currentStep = 0;
        for (int i = 0; i < Route.Count - 1; i++)
        {
            try
            {

                Coordinate coord = Route[i];
                var hz = GameObjects.map.HotZones.Where(hot => hot.Location.Equals(coord)).FirstOrDefault();

                currentStep = i + 1;
                if (hz != null)
                {
                    if (hz.RoundsTillDeathly <= currentStep)
                    {
                        val = false;
                    }
                }
            }
            catch (Exception ex)
            {
                if (Debugging.SHOW_DEBUG_INFO) HelperMethods.Error("IsThisRouteSafe method seems to run to an exception. " + ex.Message);
                if (Debugging.SHOW_DEBUG_INFO) HelperMethods.Error("IsThisRouteSafe Current iteration index is " + i.ToString() + " route.count is " + Route.Count);
            }

        }
        return val;
    }
    public static bool IsRouteDestinationStillSafe()
    {

        if (!IsSafeSpot(GameObjects.Route[GameObjects.Route.Count - 1], false))
        {
            return false;
        }
        return true;
    }
    public static void FindNewTarget(bool prioSafety = false)
    {
        List<CoordRange> ListOfPotentialSpots = new List<CoordRange>();
        Coordinate CurrentLocation = GameObjects.MyPlayer.Location;
        foreach (MapTile t in GameObjects.map.tiles.Where(tile => tile.Type == TileType.floor && tile.CanPlayerWalkOverIt == true))
        {
            if (IsSafeSpot(t.Location, false))
            {
                CoordRange cr = new CoordRange();
                cr.Location = t.Location;
                var route = GetPathFromPlayerToCoord(t.Location);
                cr.Distance = route.Count;
                //Lets check if it is accessible and how many tiles does it take to travel there. 
                if (route.Count > 0)
                {
                    if (!prioSafety)
                    {
                        var BoxCount = AmountOfEntities(t.Location, ItemAtLocation.box);
                        if (Debugging.SHOW_OBJECTS_BLAST_RADIUS) HelperMethods.Error("There is " + BoxCount + " boxes in blast radius of coordinate: " + t.Location.ToString());
                        
                        cr.Points += (GameSettings.POINTS_FROM_SOMETHING_TO_BREAK * BoxCount);

                        var playerCount = AmountOfEntities(t.Location, ItemAtLocation.player);
                        if (Debugging.SHOW_OBJECTS_BLAST_RADIUS) HelperMethods.Error("There is " + playerCount + " players in blast radius of coordinate: " + t.Location.ToString());
                        cr.Points += GameSettings.POINTS_FROM_ENEMY_PLAYER * playerCount;

                        //Is there item at this location? 
                        var items = GameObjects.map.Items.Where(item => item.Location.Equals(t.Location)).ToList().Count;
                        if (items > 0)
                        {
                            cr.Points += GameSettings.POINTS_FROM_ITEM;
                        }
                    }
                    else
                    {
                        cr.Points += GameSettings.DISTANCE_BASE_VALUE - route.Count;
                    }
                    cr.Points += GameSettings.DISTANCE_BASE_VALUE - route.Count * GameSettings.DISTANCE_MODIFIER;
                    ListOfPotentialSpots.Add(cr);
                }


            }

        }

        if (ListOfPotentialSpots.Count == 0)
        {
            if (Debugging.SHOW_DEBUG_INFO) HelperMethods.Error("There is problem with FindNewTarget function, there seems to be 0 targets to go. ");
            message = "Why are we doing this?";
            // FindJustSafeTileAndtryToGetThere();
            GameObjects.Route = new List<Coordinate>();
            GameObjects.Route.Add(GameObjects.MyPlayer.Location);
            GameObjects.RouteIndex = GameSettings.ROUTE_START_INDEX;
        }
        else if (GameObjects.Enrage == true && GameObjects.MyPlayer.InLead == false)
        {
            if (Debugging.SHOW_DEBUG_INFO) HelperMethods.Error("Player is ENRAGING, Finding nearest enemy player");
            message = "I am going to find you and kill you";
            List<Coordinate> shortestRoute = new List<Coordinate>();
            int shortestRoutecount = 100;
            int playerID = 0;
            foreach (PlayerEntity e in GameObjects.map.Players.Where(pla => pla.PlayerID != GameObjects.MyID && pla.InLead))
            {
                if (Debugging.SHOW_DEBUG_INFO) HelperMethods.Error("Checking player: " + e.PlayerID);
                var route = GetPathFromPlayerToCoord(e.Location);
                if (Debugging.SHOW_DEBUG_INFO) HelperMethods.Error(e.PlayerID + " is " + route.Count + " tiles away");
                if (route.Count <= shortestRoutecount)
                {
                    shortestRoute = route;
                    shortestRoutecount = route.Count;
                    playerID = e.PlayerID;
                }
            }
            if (Debugging.SHOW_DEBUG_INFO) HelperMethods.Error(playerID + " is my target because he is " + shortestRoute.Count + " tiles away");
            GameObjects.Route = shortestRoute;
            GameObjects.RouteIndex = GameSettings.ROUTE_START_INDEX;

        }
        else
        {
            ListOfPotentialSpots.Sort((a, b) => b.Points.CompareTo(a.Points));
            var chosenTarget = ListOfPotentialSpots.FirstOrDefault();
            GameObjects.Route = GetPathFromPlayerToCoord(chosenTarget.Location);
            GameObjects.RouteIndex = GameSettings.ROUTE_START_INDEX;
        }


    }
    public static void FindJustSafeTileAndtryToGetThere()
    {
        List<CoordRange> ListOfPotentialSpots = new List<CoordRange>();
        Coordinate CurrentLocation = GameObjects.MyPlayer.Location;
        foreach (MapTile t in GameObjects.map.tiles.Where(tile => tile.Type == TileType.floor && tile.CanPlayerWalkOverIt == true))
        {
            if (IsSafeSpot(t.Location))
            {
                CoordRange cr = new CoordRange();
                cr.Location = t.Location;
                var route = GetPathFromPlayerToCoord(t.Location);
                cr.Distance = route.Count;
                //Lets check if it is accessible and how many tiles does it take to travel there. 
                if (route.Count > 0)
                {
                    cr.Points += GameSettings.DISTANCE_BASE_VALUE - route.Count;
                    ListOfPotentialSpots.Add(cr);
                }
            }
        }
        ListOfPotentialSpots.Sort((a, b) => b.Points.CompareTo(a.Points));
        if (ListOfPotentialSpots.Count == 0)
        {
            if (Debugging.SHOW_DEBUG_INFO) HelperMethods.Error("LOL RIP");
            message = "Is it my time to go? ";
            GameObjects.Route = new List<Coordinate>();
            GameObjects.Route.Add(GameObjects.MyPlayer.Location);
            GameObjects.RouteIndex = GameSettings.ROUTE_START_INDEX;

        }
        else
        {
            ListOfPotentialSpots.Sort((a, b) => b.Points.CompareTo(a.Points));
            var chosenTarget = ListOfPotentialSpots.FirstOrDefault();
            GameObjects.Route = GetPathFromPlayerToCoord(chosenTarget.Location);
            GameObjects.RouteIndex = GameSettings.ROUTE_START_INDEX;
        }
    }
    public static CoordRange VirtualFindSafeSpot()
    {
        List<CoordRange> ListOfPotentialSpots = new List<CoordRange>();
        Coordinate CurrentLocation = GameObjects.MyPlayer.Location;
        foreach (MapTile t in GameObjects.map.tiles.Where(tile => tile.Type == TileType.floor && tile.CanPlayerWalkOverIt == true))
        {
            if (IsSafeSpot(t.Location, true))
            {
                //Lets calculate points for every possible tile. 
                CoordRange cr = new CoordRange();
                cr.Location = t.Location;

                var BoxCount = AmountOfEntities(t.Location, ItemAtLocation.box);
                cr.Points += BoxCount * GameSettings.POINTS_FROM_SOMETHING_TO_BREAK;
                //Is there item at this location? 
                var items = GameObjects.map.Items.Where(item => item.Location.Equals(t.Location)).ToList().Count;
                if (items > 0)
                {
                    cr.Points += GameSettings.POINTS_FROM_ITEM;
                }

                var route = GetPathFromPlayerToCoord(t.Location);
                cr.Distance = route.Count;

                //Lets check if it is accessible and how many tiles does it take to travel there. 
                if (route.Count > 0)
                {
                    if (Debugging.SHOW_DEBUG_INFO) HelperMethods.Error("Checking if this virtual route is safe.");
                    if (IsThisRouteSafe(route))
                    {
                        if (Debugging.SHOW_DEBUG_INFO) HelperMethods.Error("-- Yep it is x * 2 points for gryffindor");
                        cr.Points += GameSettings.POINTS_FROM_SAFETY * 2;
                    }
                    cr.Points += GameSettings.DISTANCE_BASE_VALUE - route.Count;
                    ListOfPotentialSpots.Add(cr);
                }

            }
            // is there boxes to break?
        }

        ListOfPotentialSpots.Sort((a, b) => b.Points.CompareTo(a.Points));
        var chosenTarget = ListOfPotentialSpots.FirstOrDefault();
        return chosenTarget;
    }
    public static Coordinate GetNextCoord()
    {
        try
        {
            if (Debugging.SHOW_DEBUG_INFO) HelperMethods.Error("GetNextCoord is called");
            if (GameObjects.RouteIndex == GameObjects.Route.Count)
            {
                if (Debugging.SHOW_DEBUG_INFO) HelperMethods.Error("Finding new target");
                FindNewTarget();
            }

            Coordinate NextCoord = GameObjects.Route[GameObjects.RouteIndex];

            if (IsSafeSpot(NextCoord, false))//if (IsRouteStillSafe())//
            {
                if (Debugging.SHOW_DEBUG_INFO) HelperMethods.Error("next coordinate is still safe:" + NextCoord.ToString());
                GameObjects.RouteIndex++;
                return NextCoord;
            }
            else
            {
                if (Debugging.SHOW_DEBUG_INFO) HelperMethods.Error("Next coordinate is not safe" + NextCoord.ToString());
                //Maybe we should check how long its unsafe

                if (IsSafeSpot(GameObjects.MyPlayer.Location, false))
                {
                    if (Debugging.SHOW_DEBUG_INFO) HelperMethods.Error("But my current coordinate is, staying here");
                    return GameObjects.MyPlayer.Location;
                }
                else
                {
                    //bomb which is going to kill me? 
                    if (Debugging.SHOW_DEBUG_INFO) HelperMethods.Error("And current location is not safe, Lets see what to do");
                    var thisBomb = GameObjects.map.Bombs.Where(bo => bo.TilesInBlastRadius().Where(tile => tile.Location.Equals(GameObjects.MyPlayer.Location)).ToList().Count > 0).FirstOrDefault();
                    if (GameObjects.Route.Count > thisBomb.NumberOfRoundsTillExplosion)
                    {
                        if (Debugging.SHOW_DEBUG_INFO) HelperMethods.Error("Destination is too far away,checking if alternative route is available");
                        if (Debugging.SHOW_DEBUG_INFO) HelperMethods.Error("Calling FindNewTarket and prioritizing safety");
                        FindNewTarget(true);
                        NextCoord = GameObjects.Route[GameObjects.RouteIndex];
                        GameObjects.RouteIndex++;
                        return NextCoord;
                    }
                    else
                    {
                        if (Debugging.SHOW_DEBUG_INFO) HelperMethods.Error("I can make it to the destination before detonation.");
                        NextCoord = GameObjects.Route[GameObjects.RouteIndex];
                        GameObjects.RouteIndex++;
                        if (Debugging.SHOW_DEBUG_INFO) HelperMethods.Error("Next i am heading to coordinates " + NextCoord.ToString());
                        return NextCoord;
                    }
                }
            }

        }
        catch (Exception ex)
        {
            HelperMethods.Error("GetNextCoordinate Error:" + ex.Message);
            return GameObjects.MyPlayer.Location;
        }

    }
    public static bool RouteFinished()
    {
        bool rValue = false;
        if (GameObjects.RouteIndex >= GameObjects.Route.Count)
        {
            if (Debugging.SHOW_DEBUG_INFO) HelperMethods.Error("Route finished because route.count >= route.Count which are, index = " + GameObjects.RouteIndex + " and count: " + GameObjects.Route.Count);
            rValue = true;
        }
        else if (GameObjects.Route.Count == 0)
        {
            if (Debugging.SHOW_DEBUG_INFO) HelperMethods.Error("Route finished because route.count == 0 ");
            rValue = true;
        }
        else if (GameObjects.Route[GameObjects.RouteIndex] == null)
        {
            if (Debugging.SHOW_DEBUG_INFO) HelperMethods.Error("Route finished because Route[RouteIndex] == null ");
            rValue = true;
        }
        //else if (GameObjects.RouteIndex == GameObjects.Route.Count - 1)
        //{
        //    if (Debugging.SHOW_DEBUG_INFO) HelperMethods.Error("Route finished because RouteIndex == Route.Count - 1 ");
        //    rValue = true;
        //}
        else rValue = false;
        if (Debugging.SHOW_DEBUG_INFO) HelperMethods.Error("RouteFinished function returns: " + rValue.ToString());
        return rValue;
    }
    static List<Coordinate> GetPathFromPlayerToCoord(Coordinate coord)
    {
        try
        {
            if (Debugging.SHOW_PATHFINDER_ROUTE_SEARCH) HelperMethods.Error("Finding best path for " + coord.ToString());

            bool[,] map = new bool[GameObjects.MapWidth, GameObjects.MapHeight];
            foreach (MapTile t in GameObjects.map.tiles)
            {
                map[t.Location.XCoordinate, t.Location.YCoordinate] = t.CanPlayerWalkOverIt;
            }
            Point startLocation = new Point(GameObjects.MyPlayer.Location.XCoordinate, GameObjects.MyPlayer.Location.YCoordinate);
            Point endLocation = new Point(coord.XCoordinate, coord.YCoordinate);
            SearchParameters searchparameters = new SearchParameters(startLocation, endLocation, map);
            PathFinder pathfinder = new PathFinder(searchparameters);
            List<Point> path = pathfinder.FindPath();
            if (Debugging.SHOW_PATHFINDER_ROUTE)
            {
                pathfinder.ShowRoute("Path i found", path);
            }
            List<Coordinate> PathInCoords = new List<Coordinate>();
            foreach (Point p in path)
            {
                PathInCoords.Add(new Coordinate(p.X, p.Y));
            }

            return PathInCoords;
        }
        catch (Exception ex)
        {
            HelperMethods.Error("GetPathFromPlayer function error: " + ex.Message);
            return null;
        }
    }

    public static bool IsSafeSpot(Coordinate coord, bool selfPlant = true)
    {
        if (Debugging.SHOW_DEBUG_ADVANCED) HelperMethods.Error("Checking if location " + coord.ToString() + " is in blast radius of a bomb!");
        int rad = 0;
        List<BombEntity> bombs;
        if (selfPlant)
        {
            bombs = GameObjects.map.Bombs;
        }
        else
        {
            bombs = GameObjects.map.Bombs.Where(bo => bo.NumberOfRoundsTillExplosion <= GameSettings.HOTZONE_SAFETY_LIMIT).ToList();
        }
        foreach (BombEntity bomb in bombs)
        {
            rad += bomb.TilesInBlastRadius().Where(tile => tile.Location.Equals(coord)).ToList().Count;
        }
        if (rad == 0)
        {
            return true;
        }
        else return false;
    }
    public class CoordRange
    {
        public Coordinate Location { get; set; } = new Coordinate();
        public int Points { get; set; } = 0;
        public int Distance { get; set; } = 0;
    }

    public static bool SearchFailed = false;
    static int AmountOfEntities(Coordinate coord, ItemAtLocation type = ItemAtLocation.box)
    {

        int xMax, xMin, yMax, yMin;
        xMax = HelperMethods.ClearedInt(coord.XCoordinate + (GameObjects.MyPlayer.RangeOfBombs - 1), 0, GameObjects.map.width - 1);
        xMin = HelperMethods.ClearedInt(coord.XCoordinate - (GameObjects.MyPlayer.RangeOfBombs - 1), 0, GameObjects.map.width - 1);
        yMax = HelperMethods.ClearedInt(coord.YCoordinate + (GameObjects.MyPlayer.RangeOfBombs - 1), 0, GameObjects.map.height - 1);
        yMin = HelperMethods.ClearedInt(coord.YCoordinate - (GameObjects.MyPlayer.RangeOfBombs - 1), 0, GameObjects.map.height - 1);

        int AmountOfBoxesInradius = 0;
        int amountOfItemsInRadius = 0;
        int AmountOfPlayersInRadius = 0;
        int AmountOfWallsInRadius = 0;
        //Onko xmin ja location x:n välillä seinää, itemiä tai boxia, jos on. Blast radius sieltä laidasta jää siihen. 
        //LEFT
        for (int i = coord.XCoordinate; i >= xMin; i--)
        {
            var tile = GameObjects.map.tiles.Where(xa => xa.Location.XCoordinate == i && xa.Location.YCoordinate == coord.YCoordinate).FirstOrDefault();
            if (!tile.Location.Equals(coord))
            {
                ItemAtLocation r = WhatIsThere(tile);
                if (r == ItemAtLocation.box)
                {
                    AmountOfBoxesInradius++;
                    break;
                }
                else if (r == ItemAtLocation.item)
                {
                    amountOfItemsInRadius++;
                    break;
                }
                else if (r == ItemAtLocation.player)
                {

                    AmountOfPlayersInRadius++;
                }
                else if (r == ItemAtLocation.wall)
                {
                    AmountOfWallsInRadius++;
                    break;
                }
            }
        }
        //RIGHT
        for (int i = coord.XCoordinate; i <= xMax; i++)
        {
            var tile = GameObjects.map.tiles.Where(xa => xa.Location.XCoordinate == i && xa.Location.YCoordinate == coord.YCoordinate).FirstOrDefault();
            if (!tile.Location.Equals(coord))
            {
                var r = WhatIsThere(tile);
                if (r == ItemAtLocation.box)
                {
                    AmountOfBoxesInradius++;
                    break;
                }
                else if (r == ItemAtLocation.item)
                {
                    amountOfItemsInRadius++;
                    break;
                }
                else if (r == ItemAtLocation.player)
                {
                    AmountOfPlayersInRadius++;
                }
                else if (r == ItemAtLocation.wall)
                {
                    AmountOfWallsInRadius++;
                    break;
                }
            }
        }
        //DOWN
        for (int i = coord.YCoordinate; i <= yMax; i++)
        {
            var tile = GameObjects.map.tiles.Where(xa => xa.Location.XCoordinate == coord.XCoordinate && xa.Location.YCoordinate == i).FirstOrDefault();
            if (!tile.Location.Equals(coord))
            {
                var r = WhatIsThere(tile);
                if (r == ItemAtLocation.box)
                {
                    AmountOfBoxesInradius++;
                    break;
                }
                else if (r == ItemAtLocation.item)
                {
                    amountOfItemsInRadius++;
                    break;
                }
                else if (r == ItemAtLocation.player)
                {
                    AmountOfPlayersInRadius++;
                }
                else if (r == ItemAtLocation.wall)
                {
                    AmountOfWallsInRadius++;
                    break;
                }
            }
        }
        for (int i = coord.YCoordinate; i >= yMin; i--)
        {
            var tile = GameObjects.map.tiles.Where(xa => xa.Location.XCoordinate == coord.XCoordinate && xa.Location.YCoordinate == i).FirstOrDefault();
            if (!tile.Location.Equals(coord))
            {
                var r = WhatIsThere(tile);
                if (r == ItemAtLocation.box)
                {
                    AmountOfBoxesInradius++;
                    break;
                }
                else if (r == ItemAtLocation.item)
                {
                    amountOfItemsInRadius++;
                    break;
                }
                else if (r == ItemAtLocation.player)
                {
                    AmountOfPlayersInRadius++;
                }
                else if (r == ItemAtLocation.wall)
                {
                    AmountOfWallsInRadius++;
                    break;
                }
            }
        }
        if (Debugging.SHOW_OBJECTS_BLAST_RADIUS)
        {
            HelperMethods.Error("Should plant calculation of location: " + coord.ToString());
            HelperMethods.Error("- There are " + AmountOfBoxesInradius + " boxes in radius");
            HelperMethods.Error("- There are " + amountOfItemsInRadius + " items in radius");
            HelperMethods.Error("- There are " + AmountOfWallsInRadius + " walls in radius");
            HelperMethods.Error("- There are " + AmountOfPlayersInRadius + " players in radius");
        }
        if (type == ItemAtLocation.box)
        {
            return AmountOfBoxesInradius;
        }
        else if (type == ItemAtLocation.player)
        {
            return AmountOfPlayersInRadius;
        }
        else
        {
            return AmountOfBoxesInradius;
        }

    }
    static void RampageRoute()
    {


    }
    static bool ShouldPlant()
    {

        if (GameObjects.MyPlayer.CanPlayerPlant)
        {
            if (AmountOfEntities(GameObjects.MyPlayer.Location, ItemAtLocation.box) > 0 || AmountOfEntities(GameObjects.MyPlayer.Location, ItemAtLocation.player) > 0)
            {

                return true;
            }
            if (GameObjects.map.Boxes.Count == 0)
            {
                if (!GameObjects.MyPlayer.InLead == false)
                {
                    GameObjects.Enrage = true;
                    HelperMethods.Error("Going rampage on the enemy, since no boxes are left. ");
                    foreach (PlayerEntity p in GameObjects.map.Players.Where(pla => pla.PlayerID != GameObjects.MyID))
                    {

                        if (GameObjects.MyPlayer.Location.XCoordinate == p.Location.XCoordinate || GameObjects.MyPlayer.Location.YCoordinate == p.Location.YCoordinate)
                        {

                            if (GetPathFromPlayerToCoord(p.Location).Count <= GameSettings.DISTANCE_WHEN_TO_PLANT_ON_ENEMY)
                            {
                                return true;
                            }
                        }
                    }
                }

            }
            return false;
        }
        else return false;
    }
    public enum ItemAtLocation
    {
        box = 1, player = 2, item = 3, nothing = 4, wall = 5
    }

    static ItemAtLocation WhatIsThere(MapTile t)
    {
        if (GameObjects.map.Boxes.Where(box => box.Location.Equals(t.Location)).ToList().Count > 0)
        {
            return ItemAtLocation.box;
        }
        if (GameObjects.map.Items.Where(box => box.Location.Equals(t.Location)).ToList().Count > 0)
        {
            return ItemAtLocation.item;
        }
        if (t.Type == TileType.wall)
        {
            return ItemAtLocation.wall;
        }
        if (GameObjects.map.Players.Where(box => box.Location.Equals(t.Location) && box.PlayerID != GameObjects.MyID).ToList().Count > 0)
        {
            return ItemAtLocation.player;
        }
        return ItemAtLocation.nothing;
    }

    static bool IsSafeForPlant()
    {
        // Here we make virtual plant to see what tiles would become unstable to enter, so we can find theoretically route to safety if we plant
        bool res = false;
        HelperMethods.Error("VIRTUAL: Planting Bomb on location " + GameObjects.MyPlayer.Location.ToString());
        PlantVirtualBomb();
        var safeVirtualRoute = VirtualFindSafeSpot();
        if (safeVirtualRoute != null)
        {
            HelperMethods.Error("Virtual safe spot: " + safeVirtualRoute.Location.ToString() + " Assigning it to current target, and planting the bomb. ");
            res = true;
            GameObjects.Route = GetPathFromPlayerToCoord(safeVirtualRoute.Location);
            GameObjects.RouteIndex = GameSettings.ROUTE_START_INDEX;
        }

        ClearVirtualBombs();
        HelperMethods.Error("VIRTUAL: " + res + " value of decision");
        return res;
    }
    public static void ClearVirtualBombs()
    {
        List<BombEntity> deleteThese = new List<BombEntity>();
        foreach (BombEntity b in GameObjects.map.Bombs.Where(bomb => bomb.VirtualBomb == true))
        {
            deleteThese.Add(b);
        }
        foreach (BombEntity b in deleteThese)
        {
            GameObjects.map.Bombs.Remove(b);
        }
        HelperMethods.Error("VIRTUAL: CLEARING BOMBS");

    }
    public static void PlantVirtualBomb()
    {
        BombEntity VirtualBomb = new BombEntity();
        VirtualBomb.Location = GameObjects.MyPlayer.Location;
        VirtualBomb.ExplosionRange = GameObjects.MyPlayer.RangeOfBombs;
        VirtualBomb.NumberOfRoundsTillExplosion = 4;
        VirtualBomb.PlayerID = GameObjects.MyID;
        VirtualBomb.VirtualBomb = true;
        StringBuilder st = new StringBuilder();
        foreach (MapTile c in VirtualBomb.TilesInBlastRadius())
        {
            st.Append(c.Location.ToString() + ", ");
        }
        HelperMethods.Error("If i plant in location " + VirtualBomb.Location.ToString() + " It has blastradius of these tiles: " + st.ToString());
        GameObjects.map.Bombs.Add(VirtualBomb);
    }
}

public class PlayerPoints
{
    public int playerid { get; set; }
    public int aproxpoints { get; set; } = 0;
}
public class Map
{
    public List<MapTile> tiles { get; set; } = new List<MapTile>();
    public int width { get; set; }
    public int height { get; set; }
    public PlayerEntity MyPlayer { get; set; } = new PlayerEntity();
    public List<BombEntity> Bombs { get; set; } = new List<BombEntity>();
    public List<HotZone> HotZones { get; set; } = new List<HotZone>();
    public List<PlayerEntity> Players { get; set; } = new List<PlayerEntity>();
    public List<ItemEntity> Items { get; set; } = new List<ItemEntity>();
    public List<BoxEntity> Boxes { get; set; } = new List<BoxEntity>();
}
public class MapTile
{
    public Coordinate Location { get; set; }
    public bool CanPlayerWalkOverIt
    {
        get
        {

            if (Type == TileType.wall) return false;
            //else if (GameObjects.map.HotZones.Where(l => l.Location.Equals(this.Location) && l.RoundsTillDeathly <= GameSettings.HOTZONE_SAFETY_LIMIT).ToList().Count > 0) return false;
            //else if (GameObjects.map.Bombs.Where(bomb => bomb.TilesInBlastRadius().Where(blastRad => blastRad.Location.Equals(Location)).Count() > 0).ToList().Count > 0) return false;
            else if (GameObjects.map.Bombs.Where(bomb => bomb.Location.Equals(Location)).ToList().Count > 0) return false;
            else if (GameObjects.map.Boxes.Where(bomb => bomb.Location.Equals(Location)).ToList().Count > 0) return false;
            else if (GameObjects.map.Items.Where(bomb => bomb.Location.Equals(Location)).ToList().Count > 0) return true;
            //else if (GameObjects.map.Players.Where(bomb => bomb.Location.Equals(Location)).ToList().Count > 0) return false;
            else return true;
        }
    }
    public TileType Type { get; set; }
}
public enum TileType { wall, floor, box }
public class Coordinate
{
    public int XCoordinate { get; set; }
    public int YCoordinate { get; set; }
    public Coordinate(int x, int y)
    {
        XCoordinate = x;
        YCoordinate = y;
    }
    public Coordinate()
    {

    }
    public override string ToString()
    {
        return XCoordinate.ToString() + " " + YCoordinate.ToString();
    }
    public override bool Equals(object obj)
    {
        var other = obj as Coordinate;
        if (other == null) return false;
        return Equals(other);
    }
    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
    public bool Equals(Coordinate other)
    {
        if (other == null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (this.XCoordinate != other.XCoordinate) return false;
        if (this.YCoordinate != other.YCoordinate) return false;
        return true;
    }
}
enum EntityType { Players = 0, Bombs = 1, Items = 2 }
public class Entity
{
    public Coordinate Location { get; set; } = new Coordinate();
}

public class BombEntity : Entity
{
    public bool VirtualBomb { get; set; } = false;
    public int PlayerID { get; set; }
    public int NumberOfRoundsTillExplosion { get; set; } // players --> number of bombs left. Bombs: Number of rounds left till explosion
    public int ExplosionRange { get; set; } // Players --> explosion range of the players bombs (3). bombs: Explosion range of the bomb (3)

    private bool BombGoTroughThisTile(MapTile t)
    {
        if (GameObjects.map.Boxes.Where(box => box.Location.Equals(t.Location)).ToList().Count > 0)
        {
            return false;
        }
        if (GameObjects.map.Items.Where(box => box.Location.Equals(t.Location)).ToList().Count > 0)
        {
            return true;
        }
        if (t.Type == TileType.wall)
        {
            return false;
        }
        return true;
    }
    public List<MapTile> TilesInBlastRadius()
    {
        List<MapTile> TilesInBlastRadius = new List<MapTile>();
        int xMax, xMin, yMax, yMin;
        xMax = HelperMethods.ClearedInt(Location.XCoordinate + ExplosionRange - 1, 0, GameObjects.map.width - 1);
        xMin = HelperMethods.ClearedInt(Location.XCoordinate - ExplosionRange - 1, 0, GameObjects.map.width - 1);
        yMax = HelperMethods.ClearedInt(Location.YCoordinate + ExplosionRange - 1, 0, GameObjects.map.height - 1);
        yMin = HelperMethods.ClearedInt(Location.YCoordinate - ExplosionRange - 1, 0, GameObjects.map.height - 1);


        //Onko xmin ja location x:n välillä seinää, itemiä tai boxia, jos on. Blast radius sieltä laidasta jää siihen. 
        //LEFT
        for (int i = Location.XCoordinate; i >= xMin; i--)
        {
            var tile = GameObjects.map.tiles.Where(xa => xa.Location.XCoordinate == i && xa.Location.YCoordinate == Location.YCoordinate).FirstOrDefault();
            if (BombGoTroughThisTile(tile))
            {
                if (TilesInBlastRadius.Where(te => te.Location.Equals(tile)).ToList().Count == 0)
                    TilesInBlastRadius.Add(tile);
            }
            else break;
        }
        //RIGHT
        for (int i = Location.XCoordinate; i <= xMax; i++)
        {
            var tile = GameObjects.map.tiles.Where(xa => xa.Location.XCoordinate == i && xa.Location.YCoordinate == Location.YCoordinate).FirstOrDefault();
            if (BombGoTroughThisTile(tile))
            {
                if (TilesInBlastRadius.Where(te => te.Location.Equals(tile)).ToList().Count == 0)
                    TilesInBlastRadius.Add(tile);
            }
            else break;
        }
        //DOWN
        for (int i = Location.YCoordinate; i <= yMax; i++)
        {
            var tile = GameObjects.map.tiles.Where(xa => xa.Location.XCoordinate == Location.XCoordinate && xa.Location.YCoordinate == i).FirstOrDefault();
            if (BombGoTroughThisTile(tile))
            {
                if (TilesInBlastRadius.Where(te => te.Location.Equals(tile)).ToList().Count == 0)
                    TilesInBlastRadius.Add(tile);
            }
            else break;
        }
        for (int i = Location.YCoordinate; i >= yMin; i--)
        {
            var tile = GameObjects.map.tiles.Where(xa => xa.Location.XCoordinate == Location.XCoordinate && xa.Location.YCoordinate == i).FirstOrDefault();
            if (BombGoTroughThisTile(tile))
            {
                if (TilesInBlastRadius.Where(te => te.Location.Equals(tile)).ToList().Count == 0)
                    TilesInBlastRadius.Add(tile);
            }
            else break;
        }
        return TilesInBlastRadius;
    }
}

public class PlayerEntity : Entity
{
    public int PlayerID { get; set; }
    public int NumberOfBombsLeft { get; set; }
    public int RangeOfBombs { get; set; }
    public bool InLead
    {
        get
        {
            foreach (PlayerPoints p in GameObjects.Points)
            {
                HelperMethods.Error(" - Player:" + p.playerid + " has " + p.aproxpoints);
            }
            GameObjects.Points.Sort((p1, p12) => p1.aproxpoints.CompareTo(p12.aproxpoints));
            HelperMethods.Error(GameObjects.Points.LastOrDefault().playerid + " is in lead!");
            if (GameObjects.Points.LastOrDefault().playerid == PlayerID) return true;
            else return false;
        }

    }
    public bool IsDeathlyRange
    {
        get
        {
            foreach (BombEntity bomb in GameObjects.map.Bombs)
            {
                foreach (MapTile rad in bomb.TilesInBlastRadius())
                {
                    if (rad.Location.Equals(Location))
                    {
                        HelperMethods.Error("Player seems to be in a blast radius of a bomb");
                        return true;
                    }
                }
            }
            return false;
        }
    }
    //public List<Coordinate> TilesInRange
    //{

    //    get
    //    {
    //        List<Coordinate> GoodTilesAround = new List<Coordinate>();
    //        int scanRadius = GameSettings.SEARCH_RADIUS;
    //        int xMin = 0, xMax = 0;
    //        int yMin = 0, yMax = 0;
    //        xMax = HelperMethods.ClearedInt(Location.XCoordinate + scanRadius, 0, GameObjects.map.width - 1);
    //        xMin = HelperMethods.ClearedInt(Location.XCoordinate - scanRadius, 0, GameObjects.map.width - 1);
    //        yMax = HelperMethods.ClearedInt(Location.YCoordinate + scanRadius, 0, GameObjects.map.height - 1);
    //        yMin = HelperMethods.ClearedInt(Location.YCoordinate - scanRadius, 0, GameObjects.map.height - 1);


    //        var TilesAround = GameObjects.map.tiles.Where(tile => tile.Location.XCoordinate >= xMin && tile.Location.XCoordinate <= xMax && tile.Location.YCoordinate >= yMin && tile.Location.YCoordinate <= yMax && tile.CanPlayerWalkOverIt == true).ToList();
    //        foreach (MapTile t in TilesAround)
    //        {
    //            GoodTilesAround.Add(new Coordinate(t.Location.XCoordinate, t.Location.YCoordinate));
    //        }


    //        return GoodTilesAround;
    //    }
    //}
    public int ActiveBombsByPlayer
    {
        get
        {
            return GameObjects.map.Bombs.Where(item => item.PlayerID == PlayerID).ToList().Count;
        }
    }
    public bool CanPlayerPlant
    {
        get
        {
            if (ActiveBombsByPlayer <= NumberOfBombsLeft)
                return true;
            else return false;
        }
    }
}
public class ItemEntity : Entity
{
    public ItemType itemType { get; set; }
}
public class BoxEntity : Entity
{
    public ItemType ContainsItem { get; set; }
}
public enum ItemType
{
    Empty = 0,
    ExtraRange = 1,
    ExtraBomb = 2
}
/*
 * -------------------------------------------------------------------------------------------
// Huge thanks to http://blog.two-cats.com/2014/06/a-star-example/
* --------------------------------------------------------------------------------------------------------
*/
public class Node
{
    private Node parentNode;

    public Point Location { get; private set; }

    public bool IsWalkable { get; set; }

    public float G { get; private set; }

    public float H { get; private set; }

    public NodeState State { get; set; }


    public float F
    {
        get
        {
            float points = this.G + this.H;
            //Lets check if this tile is hotzone. 
            var temp = GameObjects.map.HotZones.Where(hotz => hotz.Location.XCoordinate == Location.X && hotz.Location.YCoordinate == Location.Y).FirstOrDefault();
            if (temp != null)
            {
                int FixedBombTimer = temp.RoundsTillDeathly - ((int)this.G + 1);
                float addedValue = (GameSettings.PATHFINDER_HOTZONE_SUBSTRACTED_FROM - FixedBombTimer * GameSettings.PATHFINDER_HOTZONE_MODIFIER);
                points += addedValue;
            }
            return points;
        }
    }


    public Node ParentNode
    {
        get { return this.parentNode; }
        set
        {
            // When setting the parent, also calculate the traversal cost from the start node to here (the 'G' value)
            this.parentNode = value;
            this.G = this.parentNode.G + GetTraversalCost(this.Location, this.parentNode.Location);
        }
    }

    public Node(int x, int y, bool isWalkable, Point endLocation)
    {
        this.Location = new Point(x, y);
        this.State = NodeState.Untested;
        this.IsWalkable = isWalkable;
        this.H = GetTraversalCost(this.Location, endLocation);
        this.G = 0;
    }

    public override string ToString()
    {
        return string.Format("{0}, {1}: {2}", this.Location.X, this.Location.Y, this.State);
    }


    internal static float GetTraversalCost(Point location, Point otherLocation)
    {
        float deltaX = otherLocation.X - location.X;
        float deltaY = otherLocation.Y - location.Y;
        return (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
    }
}

public class Point
{
    public int X { get; set; }
    public int Y { get; set; }
    public Point(int x, int y)
    {
        X = x;
        Y = y;
    }
}
public class SearchParameters
{
    public Point StartLocation { get; set; }

    public Point EndLocation { get; set; }

    public bool[,] Map { get; set; }

    public SearchParameters(Point startLocation, Point endLocation, bool[,] map)
    {
        this.StartLocation = startLocation;
        this.EndLocation = endLocation;
        this.Map = map;
    }
}
public enum NodeState
{

    Untested,

    Open,

    Closed
}
public class PathFinder
{
    private int width;
    private int height;
    private Node[,] nodes;
    private Node startNode;
    private Node endNode;
    private SearchParameters searchParameters;

    /// <summary>
    /// Create a new instance of PathFinder
    /// </summary>
    /// <param name="searchParameters"></param>
    public PathFinder(SearchParameters searchParameters)
    {
        this.searchParameters = searchParameters;
        InitializeNodes(searchParameters.Map);
        this.startNode = this.nodes[searchParameters.StartLocation.X, searchParameters.StartLocation.Y];
        this.startNode.State = NodeState.Open;
        this.endNode = this.nodes[searchParameters.EndLocation.X, searchParameters.EndLocation.Y];
    }

    public List<Point> FindPath()
    {
        // The start node is the first entry in the 'open' list
        List<Point> path = new List<Point>();
        bool success = Search(startNode);
        if (success)
        {
            // If a path was found, follow the parents from the end node to build a list of locations
            Node node = this.endNode;
            while (node.ParentNode != null)
            {
                path.Add(node.Location);
                node = node.ParentNode;
            }

            // Reverse the list so it's in the correct order when returned
            path.Reverse();
        }

        return path;
    }
    public void ShowRoute(string title, IEnumerable<Point> path)
    {
        Console.Error.WriteLine("{0}\r\n", title);
        for (int y = 0; y < this.searchParameters.Map.GetLength(1) - 1; y++) // Invert the Y-axis so that coordinate 0,0 is shown in the bottom-left
        {
            for (int x = 0; x < this.searchParameters.Map.GetLength(0); x++)
            {
                if (this.searchParameters.StartLocation.Equals(new Point(x, y)))
                    // Show the start position
                    Console.Error.Write('S');
                else if (this.searchParameters.EndLocation.Equals(new Point(x, y)))
                    // Show the end position
                    Console.Error.Write('G');
                else if (this.searchParameters.Map[x, y] == false)
                    // Show any barriers
                    Console.Error.Write('#');
                else if (path.Where(p => p.X == x && p.Y == y).Any())
                    // Show the path in between
                    Console.Error.Write(' ');
                else
                    // Show nodes that aren't part of the path
                    Console.Error.Write('*');
            }

            Console.Error.WriteLine();
        }
    }

    private void InitializeNodes(bool[,] map)
    {
        this.width = map.GetLength(0);
        this.height = map.GetLength(1);
        this.nodes = new Node[this.width, this.height];
        for (int y = 0; y < this.height; y++)
        {
            for (int x = 0; x < this.width; x++)
            {
                this.nodes[x, y] = new Node(x, y, map[x, y], this.searchParameters.EndLocation);
            }
        }
    }

    private bool Search(Node currentNode)
    {
        // Set the current node to Closed since it cannot be traversed more than once
        currentNode.State = NodeState.Closed;
        List<Node> nextNodes = GetAdjacentWalkableNodes(currentNode);

        // Sort by F-value so that the shortest possible routes are considered first
        nextNodes.Sort((node1, node2) => node1.F.CompareTo(node2.F));
        foreach (var nextNode in nextNodes)
        {
            // Check whether the end node has been reached
            if (nextNode.Location == this.endNode.Location)
            {
                return true;
            }
            else
            {
                // If not, check the next set of nodes
                if (Search(nextNode)) // Note: Recurses back into Search(Node)
                    return true;
            }
        }

        // The method returns false if this path leads to be a dead end
        return false;
    }

    private List<Node> GetAdjacentWalkableNodes(Node fromNode)
    {
        List<Node> walkableNodes = new List<Node>();
        IEnumerable<Point> nextLocations = GetAdjacentLocations(fromNode.Location);

        foreach (var location in nextLocations)
        {
            int x = location.X;
            int y = location.Y;

            // Stay within the grid's boundaries
            if (x < 0 || x >= this.width || y < 0 || y >= this.height)
                continue;

            Node node = this.nodes[x, y];
            // Ignore non-walkable nodes
            if (!node.IsWalkable)
                continue;

            // Ignore already-closed nodes
            if (node.State == NodeState.Closed)
                continue;

            // Already-open nodes are only added to the list if their G-value is lower going via this route.
            if (node.State == NodeState.Open)
            {
                float traversalCost = Node.GetTraversalCost(node.Location, node.ParentNode.Location);
                float gTemp = fromNode.G + traversalCost;
                if (gTemp < node.G)
                {
                    node.ParentNode = fromNode;
                    walkableNodes.Add(node);
                }
            }
            else
            {
                // If it's untested, set the parent and flag it as 'Open' for consideration
                node.ParentNode = fromNode;
                node.State = NodeState.Open;
                walkableNodes.Add(node);
            }
        }

        return walkableNodes;
    }

    private static IEnumerable<Point> GetAdjacentLocations(Point fromLocation)
    {
        return new Point[]
        {
                new Point(fromLocation.X-1, fromLocation.Y  ),
                new Point(fromLocation.X,   fromLocation.Y+1),
                new Point(fromLocation.X+1, fromLocation.Y  ),
                new Point(fromLocation.X,   fromLocation.Y-1)
        };
    }
}
