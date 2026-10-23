using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Runtime.Startup;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Display.Colors;
using Celeste.Graphics.Rendering;
using Celeste.Graphics.Text;
using Celeste.Localization.Lookup;
using Celeste.Scene.Entities;

namespace Celeste.Mods.StrawberryJam.Cutscenes
{
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Cutscenes.Credits")]
    public partial class Credits : SceneEntityState
    {
        public static readonly PackedColor BorderColor = PackedColor.Black;
        public const float CreditSpacing = 64f;
        public const float SongLength = 174f;

        public float BottomTimer;

        private readonly ISourceEngineContext engine;
        internal ActiveBitmapFont ActiveFont => ((ISourceActiveFontScope)engine).ActiveFont;
        internal DialogCatalog Dialog => ((ISourceDialogEnvironment)engine).Dialog;
        internal float LineHeight => ActiveFont.LineHeight;
        private static Vector2 Floor(Vector2 value) => new((float)Math.Floor(value.X), (float)Math.Floor(value.Y));
        private readonly List<CreditNode> credits;
        private readonly float height;
        private readonly float alignment;
        private readonly float scale;
        private readonly AtlasEntry thanks;
        internal float scrollSpeed;
        private float scroll;

        public Credits(ISourceEngineContext engine, Vector2 position, AtlasEntry thanks, float alignment = 0.5f,
            float scale = 1f, bool doubleColumns = true)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, position)
        {
            this.engine = engine;
            this.alignment = alignment;
            this.scale = scale;
            this.thanks = thanks;
            credits = CreateCredits(doubleColumns);
            height = 0f;
            foreach (CreditNode creditNode in credits)
            {
                height += creditNode.Height(this, scale) + CreditSpacing * scale;
            }

            scrollSpeed = height / SongLength;

            Depth = -2000000;
            Tags = ((IEverestGameTagsEnvironment)engine).EverestTags.SubHUD.Value;
        }

        public override void Update()
        {
            base.Update();
            scroll += scrollSpeed * engine.DeltaTime;

            if (scroll < 0f || scroll > height)
            {
                scrollSpeed = 0f;
            }

            scroll = Math.Clamp(scroll, 0f, height);
            if (scroll >= height)
            {
                BottomTimer += engine.DeltaTime;
            }
            else
            {
                BottomTimer = 0f;
            }
        }

        public override void Render(ISourceRenderContext context)
        {
            base.Render(context);
            Vector2 nodePos = Floor(new Vector2(Position.X, 1080f - scroll));
            foreach (CreditNode creditNode in credits)
            {
                float nodeHeight = creditNode.Height(this, scale);

                if (nodePos.Y > -nodeHeight && nodePos.Y < 1080f)
                {
                    creditNode.Render(context, this, nodePos, alignment, scale);
                }

                nodePos.Y += nodeHeight + CreditSpacing * scale;
            }
        }

        private List<CreditNode> CreateCredits(bool doubleColumns)
        {
            List<CreditNode> list = new()
            {
                new Heading(this, "SJ2021_Credits_Heading_Captains"),

                new Team(this, "SJ2021_Credits_Category_Hosts", "SJ2021_Credits_Names_Captains_Hosts"),
                new Team(this, "SJ2021_Credits_Category_Mapping",
                    new Team.Section(this, "SJ2021_Credits_Category_Beginner",
                        "SJ2021_Credits_Names_Captains_Mapping_Beginner"),
                    new Team.Section(this, "SJ2021_Credits_Category_Intermediate",
                        "SJ2021_Credits_Names_Captains_Mapping_Intermediate"),
                    new Team.Section(this, "SJ2021_Credits_Category_Advanced",
                        "SJ2021_Credits_Names_Captains_Mapping_Advanced"),
                    new Team.Section(this, "SJ2021_Credits_Category_Expert",
                        "SJ2021_Credits_Names_Captains_Mapping_Expert"),
                    new Team.Section(this, "SJ2021_Credits_Category_Grandmaster",
                        "SJ2021_Credits_Names_Captains_Mapping_Grandmaster"),
                    new Team.Section(this, "SJ2021_Credits_Category_Additional",
                        "SJ2021_Credits_Names_Captains_Mapping_Additional")
                ),

                new Team(this, "SJ2021_Credits_Category_Lobby",
                    new Team.Section(this, "SJ2021_Credits_Category_Prologue",
                        "SJ2021_Credits_Names_Captains_Lobby_Prologue"),
                    new Team.Section(this, "SJ2021_Credits_Category_Beginner",
                        "SJ2021_Credits_Names_Captains_Lobby_Beginner"),
                    new Team.Section(this, "SJ2021_Credits_Category_Intermediate",
                        "SJ2021_Credits_Names_Captains_Lobby_Intermediate"),
                    new Team.Section(this, "SJ2021_Credits_Category_Advanced",
                        "SJ2021_Credits_Names_Captains_Lobby_Advanced"),
                    new Team.Section(this, "SJ2021_Credits_Category_Expert",
                        "SJ2021_Credits_Names_Captains_Lobby_Expert"),
                    new Team.Section(this, "SJ2021_Credits_Category_Grandmaster",
                        "SJ2021_Credits_Names_Captains_Lobby_Grandmaster")
                ),

                new Team(this, "SJ2021_Credits_Category_Gym", "SJ2021_Credits_Names_Captains_Gym"),
                new Team(this, "SJ2021_Credits_Category_Coding", "SJ2021_Credits_Names_Captains_Coding"),
                new Team(this, "SJ2021_Credits_Category_Art", "SJ2021_Credits_Names_Captains_Art"),
                new Team(this, "SJ2021_Credits_Category_Music", "SJ2021_Credits_Names_Captains_Music"),
                new Team(this, "SJ2021_Credits_Category_Decoration", "SJ2021_Credits_Names_Captains_Decoration"),
                new Team(this, "SJ2021_Credits_Category_Playtesting", "SJ2021_Credits_Names_Captains_Playtesting"),
                new Team(this, "SJ2021_Credits_Category_Pack_Management",
                    "SJ2021_Credits_Names_Captains_Pack_Management"),
                new Team(this, "SJ2021_Credits_Category_Media", "SJ2021_Credits_Names_Captains_Media"),
                new Team(this, "SJ2021_Credits_Category_Localization", "SJ2021_Credits_Names_Captains_Localization"),
                new Team(this, "SJ2021_Credits_Category_Accessibility", "SJ2021_Credits_Names_Captains_Accessibility"),

                new Break(180f),

                new Heading(this, "SJ2021_Credits_Heading_Teams"),

                new Team(this, "SJ2021_Credits_Category_Gameplay",
                    new Team.Section(this, "SJ2021_Credits_Category_Beginner",
                        "SJ2021_Credits_Names_Teams_Gameplay_Beginner", split: doubleColumns),
                    new Team.Section(this, "SJ2021_Credits_Category_Intermediate",
                        "SJ2021_Credits_Names_Teams_Gameplay_Intermediate", split: doubleColumns),
                    new Team.Section(this, "SJ2021_Credits_Category_Advanced",
                        "SJ2021_Credits_Names_Teams_Gameplay_Advanced", split: doubleColumns),
                    new Team.Section(this, "SJ2021_Credits_Category_Expert",
                        "SJ2021_Credits_Names_Teams_Gameplay_Expert", split: doubleColumns),
                    new Team.Section(this, "SJ2021_Credits_Category_Grandmaster",
                        "SJ2021_Credits_Names_Teams_Gameplay_Grandmaster", split: doubleColumns),
                    new Team.Section(this, "SJ2021_Credits_Category_Gym", "SJ2021_Credits_Names_Teams_Gameplay_Gym",
                        split: doubleColumns)
                ),

                new Team(this, "SJ2021_Credits_Category_Coding", "SJ2021_Credits_Names_Teams_Coding",
                    split: doubleColumns),
                new Team(this, "SJ2021_Credits_Category_Art", "SJ2021_Credits_Names_Teams_Art", split: doubleColumns),
                new Team(this, "SJ2021_Credits_Category_Music", "SJ2021_Credits_Names_Teams_Music",
                    split: doubleColumns),
                new Team(this, "SJ2021_Credits_Category_Decoration", "SJ2021_Credits_Names_Teams_Decoration",
                    split: doubleColumns),
                new Team(this, "SJ2021_Credits_Category_Playtesting", "SJ2021_Credits_Names_Teams_Playtesting",
                    split: doubleColumns),
                new Team(this, "SJ2021_Credits_Category_Overworld", "SJ2021_Credits_Names_Overworld"),
                new Team(this, "SJ2021_Credits_Category_UX_Design", "SJ2021_Credits_Names_UX_Design"),
                new Team(this, "SJ2021_Credits_Category_Localization",
                    new Team.Section(this, "SJ2021_Credits_Category_Localization_Brazilian_Portuguese",
                        "SJ2021_Credits_Names_Localization_Brazilian_Portuguese"),
                    new Team.Section(this, "SJ2021_Credits_Category_Localization_Chinese",
                        "SJ2021_Credits_Names_Localization_Chinese"),
                    new Team.Section(this, "SJ2021_Credits_Category_Localization_French",
                        "SJ2021_Credits_Names_Localization_French"),
                    new Team.Section(this, "SJ2021_Credits_Category_Localization_Japanese",
                        "SJ2021_Credits_Names_Localization_Japanese"),
                    new Team.Section(this, "SJ2021_Credits_Category_Localization_Korean",
                        "SJ2021_Credits_Names_Localization_Korean"),
                    new Team.Section(this, "SJ2021_Credits_Category_Localization_Russian",
                        "SJ2021_Credits_Names_Localization_Russian")
                ),
                new Special(this, "SJ2021_Credits_Category_Special", "SJ2021_Credits_Names_Special"),
                new Team(this, "SJ2021_Credits_Category_Helpers", "SJ2021_Credits_Names_Helpers", split: doubleColumns),

                new Thanks(this, "SJ2021_Credits_Thanks", thanks)
            };

            return list;
        }
    }
}
