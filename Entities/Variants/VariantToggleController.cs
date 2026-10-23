using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.ExtendedVariants.Configuration;
using Celeste.Mods.ExtendedVariants.Runtime;
using Celeste.Scene.Entities;

namespace Celeste.Mods.StrawberryJam.Entities.Variants
{
    [CustomEntity("SJ2021/VariantToggleController")]
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.VariantToggleController")]
    public sealed class VariantToggleController : SceneEntityState
    {
        private readonly string flag;
        private bool isFlagged;
        private readonly bool defaultValue;
        private readonly Dictionary<Variant, int> variantValues;
        private readonly VariantRuntime variants;

        public VariantToggleController(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : this(engine, data.Position + offset, data.Attr("flag", ""), data.Attr("variantList", ""),
                data.Bool("defaultValue", true))
        {
        }

        public VariantToggleController(ISourceEngineContext engine, Vector2 position, string flagName,
            string variantList, bool defValue)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, position)
        {
            flag = flagName;
            defaultValue = defValue;
            variantValues = ParseParameterList(variantList);
            variants = VariantRuntime.For(engine);
        }

        public override void Update()
        {
            base.Update();
            UpdateFlag();
        }

        public override void Awake(object scene)
        {
            isFlagged = false;
            if (!string.IsNullOrEmpty(flag))
                ((OriginalLevelSceneRoot)Scene).Session.SetFlag(flag, defaultValue);
            UpdateVariants();
        }

        private void UpdateVariants()
        {
            if (isFlagged)
            {
                foreach (KeyValuePair<Variant, int> value in variantValues)
                    variants.Enter(value.Key, value.Value, false, false, true);
                return;
            }

            foreach (KeyValuePair<Variant, int> value in variantValues)
                variants.Enter(value.Key, variants.GetDefaultValue(value.Key), false, false, false);
        }

        private void UpdateFlag()
        {
            if (!string.IsNullOrEmpty(flag) && isFlagged != ((OriginalLevelSceneRoot)Scene).Session.GetFlag(flag))
            {
                isFlagged = ((OriginalLevelSceneRoot)Scene).Session.GetFlag(flag);
                UpdateVariants();
            }
        }

        private static Dictionary<Variant, int> ParseParameterList(string list)
        {
            var result = new Dictionary<Variant, int>();
            if (string.IsNullOrEmpty(list)) return result;
            string[] entries = list.Split(',');
            for (int i = 0; i < entries.Length; i++)
            {
                string[] parts = entries[i].Split(':');
                if (parts.Length >= 2)
                    result[(Variant)Enum.Parse(typeof(Variant), parts[0])] = int.Parse(parts[1]);
            }

            return result;
        }
    }
}
