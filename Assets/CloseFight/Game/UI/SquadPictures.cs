using System.Collections.Generic;
using CloseFight.Core.Units;
using UnityEngine;

namespace CloseFight.Game.UI
{
    /// <summary>What a man carries, as the roster draws him (M18).</summary>
    public enum FigureRole : byte
    {
        Rifleman,
        /// <summary>Squad or team leader with a submachine gun.</summary>
        Leader,
        /// <summary>Light machine gunner (MG 34 on its bipod, DP).</summary>
        MachineGunner,
        /// <summary>The MG's No. 2: pistol and ammunition boxes.</summary>
        Assistant,
        /// <summary>Heavy machine gun on its tripod or wheels (sMG 34, Maxim).</summary>
        HeavyGunner,
        Sniper,
        Flamethrower,
        /// <summary>Radio operator of an artillery observer team.</summary>
        Radio,
        /// <summary>Gun or vehicle crew: no long weapon.</summary>
        Crew,
    }

    /// <summary>
    /// Unit pictures for the roster (M18), in the manner of the later Close Combat unit
    /// tiles: one colour picture per kind of unit and side, shared by every unit of that
    /// kind, showing the men at full strength, each doing his job (the leader with
    /// binoculars, the gunner carrying his MG). Vehicles show the vehicle; gun teams the
    /// gun with its crew. The exact count is in the heads beside it, one per man: green
    /// unhurt, yellow lightly wounded, light red badly wounded, dark red dead, grey gone.
    ///
    /// Pictures come from Resources/Art/Roster/unit_&lt;de|su&gt;_&lt;kind&gt; (infantry) and
    /// unit_&lt;equipment id&gt; (vehicles and guns), made by tools/roster/ with a local image
    /// model. Until one exists, temporary silhouettes composed from placeholder figures
    /// stand in.
    /// </summary>
    public static class SquadPictures
    {
        const int FigureWidth = 40;
        const int FigureHeight = 92;

        static readonly Dictionary<string, Texture2D> Figures = new Dictionary<string, Texture2D>();
        static readonly Dictionary<Team, FigureRole[]> Roles = new Dictionary<Team, FigureRole[]>();
        static readonly Dictionary<string, Texture2D> Equipment = new Dictionary<string, Texture2D>();
        static Texture2D _head;

        public static readonly Color Unhurt = new Color(0.35f, 0.8f, 0.3f);
        public static readonly Color LightlyWounded = new Color(0.95f, 0.85f, 0.2f);
        public static readonly Color BadlyWounded = new Color(1f, 0.52f, 0.47f);
        public static readonly Color Dead = new Color(0.55f, 0.06f, 0.06f);
        public static readonly Color Gone = new Color(0.5f, 0.5f, 0.5f);

        public static Color HeadColor(SoldierStatus status)
        {
            switch (status)
            {
                case SoldierStatus.Wounded: return LightlyWounded;
                case SoldierStatus.Incapacitated: return BadlyWounded;
                case SoldierStatus.Dead: return Dead;
                case SoldierStatus.Fled: return Gone;
                default: return Unhurt;
            }
        }

        /// <summary>The figure for seat <paramref name="index"/> of a team, by the weapon the tables issue (not what he picked up since).</summary>
        public static FigureRole RoleFor(Side side, TeamType type, int index)
        {
            if (type == TeamType.Vehicle || type == TeamType.AntiTankGun || type == TeamType.Mortar)
            {
                return FigureRole.Crew;
            }

            if (type == TeamType.Observer && index == 1)
            {
                return FigureRole.Radio;
            }

            var weapon = Weapons.Issue(side, type, index);
            switch (weapon.Class)
            {
                case WeaponClass.SubmachineGun: return FigureRole.Leader;
                case WeaponClass.MachineGun: return type == TeamType.MachineGun ? FigureRole.HeavyGunner : FigureRole.MachineGunner;
                case WeaponClass.Pistol: return FigureRole.Assistant;
                case WeaponClass.SniperRifle: return FigureRole.Sniper;
                case WeaponClass.Flamethrower: return FigureRole.Flamethrower;
                default: return FigureRole.Rifleman;
            }
        }

        static FigureRole[] RolesOf(Team team)
        {
            if (!Roles.TryGetValue(team, out var roles))
            {
                roles = new FigureRole[team.Members.Count];
                for (int i = 0; i < roles.Length; i++)
                {
                    roles[i] = RoleFor(team.Side, team.Type, i);
                }

                Roles[team] = roles;
            }

            return roles;
        }

        /// <summary>The temporary silhouettes are drawn in near-black on the roster's olive tile.</summary>
        static readonly Color Ink = new Color(0.06f, 0.06f, 0.05f, 1f);

        static readonly Dictionary<string, Texture2D> Pictures = new Dictionary<string, Texture2D>();

        /// <summary>The infantry kinds that have a picture of their own, by team type.</summary>
        public static string KindName(TeamType type)
        {
            switch (type)
            {
                case TeamType.Headquarters: return "hq";
                case TeamType.MachineGun: return "hmg";
                case TeamType.Mortar: return "mortar";
                case TeamType.AntiTankGun: return "atgun";
                case TeamType.Sniper: return "sniper";
                case TeamType.Engineer: return "engineer";
                case TeamType.Observer: return "observer";
                case TeamType.Vehicle: return "vehicle";
                default: return "rifle";
            }
        }

        /// <summary>
        /// The picture's name under Resources/Art/Roster: unit_&lt;equipment id&gt; for a vehicle
        /// or gun team (the gun with its crew), else unit_&lt;de|su&gt;_&lt;kind&gt;.
        /// </summary>
        public static string PictureKey(Team team)
        {
            if (team.Vehicle != null) return "unit_" + team.Vehicle.Definition.Id;
            if (team.Gun != null) return "unit_" + team.Gun.Definition.Id;
            return "unit_" + (team.Side == Side.German ? "de_" : "su_") + KindName(team.Type);
        }

        /// <summary>A picture from Resources/Art/Roster, or null; misses are remembered too.</summary>
        public static Texture2D Picture(string key)
        {
            if (!Pictures.TryGetValue(key, out var texture))
            {
                texture = Resources.Load<Texture2D>("Art/Roster/" + key);
                Pictures[key] = texture;
            }

            return texture;
        }

        /// <summary>Draws the team's picture inside <paramref name="area"/> (GUI coordinates).</summary>
        public static void Draw(Rect area, Team team)
        {
            var picture = Picture(PictureKey(team));
            if (picture != null)
            {
                UiArt.DrawPicture(area, picture);
                return;
            }

            // Temporary silhouettes until the picture exists.
            if (team.Vehicle != null)
            {
                DrawEquipment(area, team.Vehicle.Definition.Id);
                return;
            }

            var roles = RolesOf(team);
            if (team.Gun != null)
            {
                // The gun on the left, its crew beside it.
                DrawEquipment(new Rect(area.x, area.y + 2f, area.width * 0.52f, area.height - 2f), team.Gun.Definition.Id);
                DrawRanks(new Rect(area.x + area.width * 0.5f, area.y, area.width * 0.5f, area.height), team.Side, roles);
                return;
            }

            DrawRanks(area, team.Side, roles);
        }

        /// <summary>
        /// A vehicle or gun facing right, as a silhouette: Codex roster art (equip_&lt;id&gt;), or until
        /// then a thumbnail baked from the top-down art by tools/roster/make_equipment_thumbs.py.
        /// </summary>
        public static void DrawEquipment(Rect area, string id)
        {
            if (!Equipment.TryGetValue(id, out var texture))
            {
                texture = Resources.Load<Texture2D>("Art/Roster/equip_" + id);
                Equipment[id] = texture;
            }

            if (texture != null)
            {
                GUI.color = Ink;
                GUI.DrawTexture(area, texture, ScaleMode.ScaleToFit, true);
                GUI.color = Color.white;
            }
        }

        /// <summary>At most this many figures in a picture: the heads beside it give the exact count.</summary>
        public const int MaxFigures = 5;

        /// <summary>
        /// One rank of silhouettes, the leader first. Like the recent Close Combat tiles it shows
        /// who does what (the first seats of the table: leader, machine gunner, his No. 2,
        /// riflemen) rather than every man: at this size ten figures merge into one blot.
        /// </summary>
        public static void DrawRanks(Rect area, Side side, FigureRole[] roles)
        {
            int count = Mathf.Min(roles.Length, MaxFigures);
            float height = area.height * 0.96f;
            DrawRank(new Rect(area.x, area.yMax - height, area.width, height), side, roles, 0, count, height, Ink);
        }

        static void DrawRank(Rect rank, Side side, FigureRole[] roles, int first, int count, float height, Color ink)
        {
            float width = height * FigureWidth / FigureHeight;
            float step = count > 1 ? Mathf.Min(width * 1.05f, (rank.width - width) / (count - 1)) : 0f;
            rank.x += (rank.width - (width + step * (count - 1))) * 0.5f;
            GUI.color = ink;
            for (int i = 0; i < count; i++)
            {
                var texture = Figure(side, roles[first + i]);
                GUI.DrawTexture(new Rect(rank.x + i * step, rank.y, width, height), texture, ScaleMode.StretchToFill, true);
            }

            GUI.color = Color.white;
        }

        /// <summary>One head per man, coloured by his state (CC style).</summary>
        public static void DrawHeads(Rect area, Team team, float headWidth = 10f, float headHeight = 13f)
        {
            var head = Head();
            float spacing = headWidth + 1f;
            float step = Mathf.Min(spacing, team.Members.Count > 1 ? (area.width - headWidth) / (team.Members.Count - 1) : spacing);
            for (int i = 0; i < team.Members.Count; i++)
            {
                GUI.color = HeadColor(team.Members[i].Status);
                GUI.DrawTexture(new Rect(area.x + i * step, area.y, headWidth, headHeight), head, ScaleMode.StretchToFill, true);
            }

            GUI.color = Color.white;
        }

        // ------------------------------------------------------------ art

        static Texture2D Figure(Side side, FigureRole role)
        {
            string key = (side == Side.German ? "de_" : "su_") + RoleName(role);
            if (!Figures.TryGetValue(key, out var texture) || texture == null)
            {
                texture = Resources.Load<Texture2D>("Art/Roster/fig_" + key);
                if (texture == null)
                {
                    texture = PlaceholderFigures.Draw(side, role, FigureWidth, FigureHeight);
                }

                Figures[key] = texture;
            }

            return texture;
        }

        public static string RoleName(FigureRole role)
        {
            switch (role)
            {
                case FigureRole.Leader: return "leader";
                case FigureRole.MachineGunner: return "lmg";
                case FigureRole.Assistant: return "assistant";
                case FigureRole.HeavyGunner: return "hmg";
                case FigureRole.Sniper: return "sniper";
                case FigureRole.Flamethrower: return "flamethrower";
                case FigureRole.Radio: return "radio";
                case FigureRole.Crew: return "crew";
                default: return "rifleman";
            }
        }

        /// <summary>A small head-and-shoulders silhouette in white, tinted when drawn: Codex art (Art/Roster/head) or a drawn stand-in.</summary>
        static Texture2D Head()
        {
            if (_head != null)
            {
                return _head;
            }

            _head = Resources.Load<Texture2D>("Art/Roster/head");
            if (_head != null)
            {
                return _head;
            }

            const int w = 20, h = 26;
            var pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Texture rows run bottom-up; work top-down.
                    float fx = x + 0.5f, fy = h - y - 0.5f;
                    float headD = Vector2.Distance(new Vector2(fx, fy), new Vector2(10f, 8f));
                    bool shoulders = fy > 16f && (fx - 10f) * (fx - 10f) / 100f + (fy - 26f) * (fy - 26f) / 100f <= 1f;
                    float a = Mathf.Clamp01(7.5f - headD);
                    if (shoulders) a = 1f;
                    pixels[y * w + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            }

            _head = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = "RosterHead", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            _head.SetPixels32(pixels);
            _head.Apply(false, true);
            return _head;
        }
    }
}
