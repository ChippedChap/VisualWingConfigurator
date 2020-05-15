using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace VisualWingConfigurator
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    class WingWindow : MonoBehaviour
    {
        private Part currentPart;

        private PopupDialog window;

        private readonly WingStats main = new WingStats();
        private readonly WingStats srf = new WingStats(0.5f);

        private float deltaSize = 0.1f;
        private bool showHits = false;
        private bool controlSurface = false;

        private Vector3 debugRootLeadHit;
        private Vector3 debugRootTrailHit;
        private Vector3 debugTipLeadHit;
        private Vector3 debugTipTrailHit;

        private readonly Vector2 margins = new Vector2(30f, 30f);

        public string CurrentPartName
        {
            get
            {
                return currentPart ? currentPart.name : "No part selected";
            }
        }

        private static string Clipboard
        {
            set
            {
                TextEditor editor = new TextEditor
                {
                    text = value
                };
                editor.SelectAll();
                editor.Copy();
            }
        }

        public void Update()
        {
            if (GameSettings.MODIFIER_KEY.GetKey() && Input.GetKeyDown(KeyCode.O))
            {
                if (window)
                {
                    InputLockManager.RemoveControlLock("LockGizmosWhileVisualWingConfiguratorIsOpen");
                    InputLockManager.RemoveControlLock("LockRerootWhileVisualWingConfiguratorIsOpen");
                    window.Dismiss();
                    window = null;
                }
                else
                {
                    InputLockManager.SetControlLock(ControlTypes.EDITOR_GIZMO_TOOLS, "LockGizmosWhileVisualWingConfiguratorIsOpen");
                    InputLockManager.SetControlLock(ControlTypes.EDITOR_ROOT_REFLOW, "LockRerootWhileVisualWingConfiguratorIsOpen");
                    window = BuildWindow();
                }
            }
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                currentPart = Mouse.HoveredPart;
            }
        }

        public void OnRenderObject()
        {
            if (currentPart && window)
            {
                DrawLines(currentPart, main, Color.magenta);
                if(controlSurface) DrawLines(currentPart, srf, Color.cyan, false);
                if(showHits)
                {
                    DrawTools.DrawPoint(debugRootLeadHit, Color.red);
                    DrawTools.DrawPoint(debugRootTrailHit, Color.blue);
                    DrawTools.DrawPoint(debugTipLeadHit, Color.green);
                    DrawTools.DrawPoint(debugTipTrailHit, Color.cyan);
                }
            }
        }

        private PopupDialog BuildWindow()
        {
            Debug.Log("[VisualWingConfigurator] Creating window");
            Vector2 dimensions = new Vector2(420f, 600f);
            float labelWidth = dimensions.x / 2;
            var dialogs = new List<DialogGUIBase>();

            var status = new DialogGUIVerticalLayout();
            dialogs.Add(status);
            status.AddChild(DisplayStatDialog(labelWidth, "Highlighted Part: ", () => { return Mouse.HoveredPart ? Mouse.HoveredPart.name : "No part highlighted"; }));
            status.AddChild(DisplayStatDialog(labelWidth, "Selected Part: ", () => { return CurrentPartName; }));

            // Change increment amount
            dialogs.Add(SetFloatDialog(labelWidth, "Increment Amount: ", () => { return Format(deltaSize); },
                (string input) => { return ProcessTextToFloat(input, ref deltaSize); },
                () => { return 10f; }, (float change) => { deltaSize *= (change > 0) ? change : -1 / change; }));

            dialogs.Add(new DialogGUISpace(5f));
            dialogs.Add(DrawWingSettings(labelWidth, main));
            dialogs.Add(new DialogGUISpace(5f));

            var results = new DialogGUIVerticalLayout();
            dialogs.Add(results);
            results.AddChild(DisplayStatDialog(labelWidth, "b_2", () => { return Format(main.B_2); }));
            results.AddChild(DisplayStatDialog(labelWidth, "MAC", () => { return Format(main.MAC); }));
            results.AddChild(DisplayStatDialog(labelWidth, "TaperRatio", () => { return Format(main.TaperRatio); }));
            results.AddChild(DisplayStatDialog(labelWidth, "MidChordSweep", () => { return Format(main.MidChordSweep); }));
            results.AddChild(DisplayStatDialog(labelWidth, "rootMidChordOffsetFromOrig", () => { return main.rootMidChordOffset.ToString("0.000"); }));

            dialogs.Add(new DialogGUISpace(5f));

            // Control surface settings - should only show up if controlSurface is true.
            var srfControl = new DialogGUIFlexibleSpace();
            srfControl.OptionEnabledCondition = () => { return controlSurface; };
            dialogs.Add(srfControl);
            srfControl.AddChild(DrawWingSettings(labelWidth, srf));
            srfControl.AddChild(new DialogGUISpace(5f));
            srfControl.AddChild(DisplayStatDialog(labelWidth, "ctrlSurfFrac", () => { return Format(srf.Area / main.Area); }));

            var copyconfig = new DialogGUIHorizontalLayout();
            dialogs.Add(copyconfig);
            copyconfig.AddChild(new DialogGUIButton("Copy MM Config to Clipboard", () => { Clipboard = GetFullMMConfig(controlSurface); }, labelWidth, -1f, false));
            copyconfig.AddChild(new DialogGUIToggleButton(() => { return controlSurface; }, "Is Control Surface", (bool b) => { controlSurface = b; }, -1, -1));

            var autocalc = new DialogGUIHorizontalLayout();
            dialogs.Add(autocalc);
            autocalc.AddChild(new DialogGUIButton("Try Autocalculating Lengths", () => { TryAutocalculateChordLength(); }, labelWidth, -1f, false));
            autocalc.AddChild(new DialogGUIToggleButton(() => { return showHits; }, "Show Raycast Hits", (bool b) => { showHits = b; }, -1, -1));

            dialogs.Add(new DialogGUIButton("Reset", () => 
            {
                main.Reset(1f);
                srf.Reset(0.5f);
            }, labelWidth, -1f, false));

            return PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new MultiOptionDialog("main", "", "", HighLogic.UISkin, new Rect(0.75f, 0.5f, dimensions.x, dimensions.y), dialogs.ToArray()),
                false, UISkinManager.defaultSkin, false);
        }

        private DialogGUIVerticalLayout DrawWingSettings(float labelWidth, WingStats wing, bool alwaysOn = true, Func<bool> isOn = null)
        {
            var settings = new DialogGUIVerticalLayout();

            // Vertical and root settings
            settings.AddChild(SetFloatDialog(labelWidth, "Vertical Offset: ", () => { return Format(wing.ZOffset); },
                (string input) => { return ProcessTextToFloat(input, wing.ZOffset, (float s) => { wing.ZOffset = s; }); },
                () => { return deltaSize; }, (float change) => { wing.ZOffset += change; }));
            settings.AddChild(SetFloatDialog(labelWidth, "Length of Root Chord: ", () => { return Format(wing.rootChordLength); },
                (string input) => { return ProcessTextToFloat(input, ref wing.rootChordLength); },
                () => { return deltaSize; }, (float change) => { wing.rootChordLength += change; }));
            settings.AddChild(SetFloatDialog(labelWidth, "Offset of Root Midpoint - X (Right+)", () => { return Format(wing.rootMidChordOffset.x); },
                (string input) => { return ProcessTextToFloat(input, ref wing.rootMidChordOffset.x); },
                () => { return deltaSize; }, (float change) => { wing.rootMidChordOffset.x += change; }));
            settings.AddChild(SetFloatDialog(labelWidth, "Offset of Root Midpoint - Y (Forward+)", () => { return Format(wing.rootMidChordOffset.y); },
                (string input) => { return ProcessTextToFloat(input, ref wing.rootMidChordOffset.y); },
                () => { return deltaSize; }, (float change) => { wing.rootMidChordOffset.y += change; }));

            settings.AddChild(new DialogGUISpace(5f));

            // Tip settings
            settings.AddChild(SetFloatDialog(labelWidth, "Length of Tip Chord: ", () => { return Format(wing.tipChordLength); },
                (string input) => { return ProcessTextToFloat(input, ref wing.tipChordLength); },
                () => { return deltaSize; }, (float change) => { wing.tipChordLength += change; }));
            settings.AddChild(SetFloatDialog(labelWidth, "Offset of Tip Midpoint - X (Right+)", () => { return Format(wing.tipMidChordOffset.x); },
                (string input) => { return ProcessTextToFloat(input, ref wing.tipMidChordOffset.x); },
                () => { return deltaSize; }, (float change) => { wing.tipMidChordOffset.x += change; }));
            settings.AddChild(SetFloatDialog(labelWidth, "Offset of Tip Midpoint - Y (Forward+)", () => { return Format(wing.tipMidChordOffset.y); },
                (string input) => { return ProcessTextToFloat(input, ref wing.tipMidChordOffset.y); },
                () => { return deltaSize; }, (float change) => { wing.tipMidChordOffset.y += change; }));

            if(!alwaysOn)
                foreach(DialogGUIBase dialog in settings.children)
                    settings.OptionEnabledCondition = isOn;

            return settings;
        }

        private void DrawLines(Part draw, WingStats wing, Color color, bool drawTransform = true)
        {
            Vector3 leadRoot = draw.transform.position + draw.transform.TransformDirection(wing.rootMidChordOffset + Vector3.up * wing.rootChordLength / 2);
            Vector3 trailRoot = draw.transform.position + draw.transform.TransformDirection(wing.rootMidChordOffset - Vector3.up * wing.rootChordLength / 2);

            Vector3 leadTip = draw.transform.position + draw.transform.TransformDirection(wing.tipMidChordOffset + Vector3.up * wing.tipChordLength / 2);
            Vector3 trailTip = draw.transform.position + draw.transform.TransformDirection(wing.tipMidChordOffset - Vector3.up * wing.tipChordLength / 2);

            DrawTools.DrawLine(leadRoot, trailRoot, color);
            DrawTools.DrawLine(leadTip, trailTip, color);
            if(drawTransform) DrawTools.DrawTransform(draw.transform, 0.25f);
        }

        private DialogGUIBase DisplayStatDialog(float labelWidth, string title, System.Func<string> display)
        {
            var horizontal = new DialogGUIHorizontalLayout();
            horizontal.AddChild(new DialogGUILabel(false, () => { return title; }, labelWidth));
            horizontal.AddChild(new DialogGUILabel(display));
            return horizontal;
        }

        private DialogGUIBase SetFloatDialog(float labelWidth, string title, Func<string> currentValue,
            Func<string, string> processor, Func<float> incSize, Callback<float> increment)
        {
            var setDialog = new DialogGUIHorizontalLayout();
            setDialog.AddChild(new DialogGUILabel(false, () => { return title; }, labelWidth));
            setDialog.AddChild(new DialogGUIButton("-", () => { increment(-incSize()); }, false));
            setDialog.AddChild(new DialogGUITextInput(currentValue(), false, 10, processor, currentValue, TMP_InputField.ContentType.DecimalNumber));
            setDialog.AddChild(new DialogGUIButton("+", () => { increment(incSize()); }, false));
            return setDialog;
        }

        private string ProcessTextToFloat(string input, ref float previous)
        {
            if (float.TryParse(input, out previous))
                return input;
            else
                return previous.ToString();
        }

        private string ProcessTextToFloat(string input, float previous, Callback<float> set)
        {
            if (float.TryParse(input, out float output))
            {
                set(output);
                return input;
            }
            else
            {
                return Format(previous);
            }
        }

        private string Format(float number)
        {
            return number.ToString("0.000");
        }

        // Patch format follows that in FerramAerospaceResearch.cfg
        private string GetFullMMConfig(bool controlSurface = false)
        {
            if(!currentPart)
            {
                ScreenMessages.PostScreenMessage("No part selected.");
                return "currentPart IS NULL";
            }
            var patch = new ConfigNode("@PART[" + currentPart.name + "]:NEEDS[FerramAerospaceResearch|NEAR]:FOR[FerramAerospaceResearch]");
            patch.AddValue("@maximum_drag", 0);
            patch.AddValue("@minimum_drag", 0);
            patch.AddValue("@angularDrag", 0);

            patch.AddNode("!MODULE[ModuleLiftingSurface]");
            patch.AddNode("!MODULE[ModuleControlSurface]");
            patch.AddNode("!MODULE[ModuleAeroSurface]");
            
            patch.AddNode("%MODULE[" + (controlSurface ? "FARControllableSurface" : "FARWingAerodynamicModel") + "]");
            var farModule = patch.GetNode("%MODULE[" + (controlSurface ? "FARControllableSurface" : "FARWingAerodynamicModel") + "]");
            farModule.AddValue("%b_2", main.B_2);
            farModule.AddValue("%MAC", main.MAC);
            farModule.AddValue("%TaperRatio", main.TaperRatio);
            farModule.AddValue("%MidChordSweep", main.MidChordSweep);
            if(controlSurface)
            {
                farModule.AddValue("%nonSideAttach", 1);
                farModule.AddValue("%maxDeflect", 20);
                farModule.AddValue("%ctrlSurfFrac", srf.Area / main.Area);
                farModule.AddValue("%transformName", "", "This needs to be filled manually. Using sarbian's DebugStuff is advised.");
            }
            return patch.ToString();
        }

        private void TryAutocalculateChordLength(float rayDistance = 50f)
        {
            if (!currentPart)
            {
                ScreenMessages.PostScreenMessage("No part selected.");
                return;
            }
            // Switch colliders to different layer and remember their previous layers.
            currentPart.collider.gameObject.layer = 30;
            Collider[] partColliders = currentPart.GetPartColliders();
            int[] layers = new int[partColliders.Length];
            for (int i = 0; i < partColliders.Length; i++)
            {
                layers[i] = partColliders[i].gameObject.layer;
                partColliders[i].gameObject.layer = 30;
            }
            // Root lengths
            Vector3 leadRootOrigin = currentPart.transform.TransformPoint(main.rootMidChordOffset + Vector3.up * rayDistance);
            Vector3 trailRootOrigin = currentPart.transform.TransformPoint(main.rootMidChordOffset - Vector3.up * rayDistance);
            if(Physics.Raycast(leadRootOrigin, -currentPart.transform.up, out RaycastHit leadHit, 2 * rayDistance, 1 << 30) &&
                Physics.Raycast(trailRootOrigin, currentPart.transform.up, out RaycastHit trailHit, 2 * rayDistance, 1 << 30))
            {
                debugRootLeadHit = leadHit.point;
                debugRootTrailHit = trailHit.point;
                main.rootMidChordOffset = currentPart.transform.InverseTransformPoint(Vector3.Lerp(debugRootLeadHit, debugRootTrailHit, 0.5f));
                main.rootChordLength = (debugRootLeadHit - debugRootTrailHit).magnitude;
            }
            else
            {
                ScreenMessages.PostScreenMessage("No root detected.");
                debugRootLeadHit = debugRootTrailHit = Vector3.zero;
            }
            // Tip lengths
            Vector3 leadTipOrigin = currentPart.transform.TransformPoint(main.tipMidChordOffset + Vector3.up * rayDistance);
            Vector3 trailTipOrigin = currentPart.transform.TransformPoint(main.tipMidChordOffset - Vector3.up * rayDistance);
            if(Physics.Raycast(leadTipOrigin, -currentPart.transform.up, out RaycastHit leadTipHit, 2 * rayDistance, 1 << 30) &&
                Physics.Raycast(trailTipOrigin, currentPart.transform.up, out RaycastHit trailTipHit, 2 * rayDistance, 1 << 30))
            {
                debugTipLeadHit = leadTipHit.point;
                debugTipTrailHit = trailTipHit.point;
                main.tipMidChordOffset = currentPart.transform.InverseTransformPoint(Vector3.Lerp(debugTipLeadHit, debugTipTrailHit, 0.5f));
                main.tipChordLength = (debugTipLeadHit - debugTipTrailHit).magnitude;
            }
            else
            {
                ScreenMessages.PostScreenMessage("No tip detected.");
                debugTipLeadHit = debugTipTrailHit = Vector3.zero;
            }
            // Switch back layers
            for (int i = 0; i < partColliders.Length; i++) partColliders[i].gameObject.layer = layers[i];
        }
    }
}
