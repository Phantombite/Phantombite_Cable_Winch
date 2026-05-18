using System;
using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;

namespace Phantombite.CableWinch
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_ExtendedPistonBase), false,
        "CableWinch_Base", "SG_CableWinch_Base", "CableWinch_Base_TINY")]
    public class CableWinchLogic : MyGameLogicComponent
    {
        // Subpart- und Dummy-Namen — hardcoded im MWM-Modell, nicht ändern
        private const string RotorSubpartName     = "Rotor1";
        private const string Segment1SubpartName  = "PistonSubpart1";
        private const string Segment2SubpartName  = "PistonSubpart2";
        private const string Segment3SubpartName  = "PistonSubpart3";
        private const string CableDummyName       = "PulleyWire";

        // SG_CableWinch_Base ist ein 3x3x3 Small-Grid-Block
        private static readonly Vector3I MediumBlockSize = new Vector3I(3, 3, 3);

        private IMyExtendedPistonBase _winch;

        // Subparts
        private MyEntitySubpart _rotor;
        private MyEntitySubpart _segment1;
        private MyEntitySubpart _segment2;
        private MyEntitySubpart _segment3;

        // Kabel-Konfiguration (wird einmalig in UpdateOnceBeforeFrame gesetzt)
        private string     _cableModel;
        private float      _segmentLength;
        private MyStringId _ropeId;

        // Draw-Modus:
        //  -1 = noch nicht initialisiert
        //   0 = Fallback: einfache Linie (wenn >2 Dummy-Paare gefunden)
        //   1 = ein physisches Kabel (PulleyWire_1)
        //   2 = zwei physische Kabel  (PulleyWire_1 + PulleyWire_2)
        private int _drawMode = -1;

        // Dummy-Punkte für physische Kabel (Mode 1 + 2)
        private IMyModelDummy _cableStart1;
        private IMyModelDummy _cableEnd1;
        private IMyModelDummy _cableStart2;
        private IMyModelDummy _cableEnd2;

        // Dummy-Paare für Linien-Fallback (Mode 0)
        private List<KeyValuePair<IMyModelDummy, IMyModelDummy>> _linePairs;
        private Vector4 _ropeColor = Vector4.One;

        // Gespawnte Kabel-Segmente
        private List<MyEntity> _cableSegments1 = new List<MyEntity>();
        private List<MyEntity> _cableSegments2 = new List<MyEntity>();

        // Eingefrorene Positionen wenn Block nicht funktional
        private Vector3D _freezePos1 = Vector3D.Zero;
        private Vector3D _freezePos2 = Vector3D.Zero;
        private bool     _frozen;

        // Rotor-Animation
        private float _lastPistonPos;

        // Ob Init-Phase abgeschlossen ist
        private bool _initialized;

        // PerfLevel — von CableWinchSession gesetzt
        public static int PerfLevel { get; set; } = 0;

        // =====================================================================
        // LIFECYCLE
        // =====================================================================

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (MyAPIGateway.Utilities.IsDedicated)
                return;

            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            _winch = Entity as IMyExtendedPistonBase;
            if (_winch == null)
                return;

            var block = _winch as MyCubeBlock;
            if (block == null)
                return;

            string modPath = block.BlockDefinition.Context.ModPath;

            if (block.BlockDefinition.CubeSize == MyCubeSize.Large)
            {
                _cableModel    = modPath + @"\Models\Cubes\large\CableWinch_Cable.mwm";
                _segmentLength = 1.25f;
            }
            else if (block.BlockDefinition.Size == MediumBlockSize)
            {
                _cableModel    = modPath + @"\Models\Cubes\small\CableWinch_Cable_Medium.mwm";
                _segmentLength = 0.75f;
            }
            else
            {
                _cableModel    = modPath + @"\Models\Cubes\small\CableWinch_Cable_Tiny.mwm";
                _segmentLength = 0.25f;
            }

            _ropeId = MyStringId.GetOrCompute("rope");

            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
        }

        public override void UpdateBeforeSimulation()
        {
            if (_winch == null || _winch.MarkedForClose || _winch.Closed)
                return;

            if (_winch.CubeGrid?.Physics == null)
                return;

            if (PerfLevel >= 3)
            {
                if (_initialized && _cableSegments1.Count > 0)
                {
                    CleanupSegments(_cableSegments1);
                    CleanupSegments(_cableSegments2);
                }
                return;
            }

            if (!_initialized)
            {
                if (!TryGetSubparts() || !TryGetDummies())
                    return;
                _initialized = true;
            }

            if (!_winch.IsFunctional)
            {
                _frozen = true;
                FreezeCables();
                return;
            }

            if (_frozen)
            {
                _frozen = false;
                if (!TryGetSubparts() || !TryGetDummies())
                    return;
            }

            DrawCables();
            AnimateRotor();
        }

        public override void Close()
        {
            if (MyAPIGateway.Utilities.IsDedicated)
                return;

            NeedsUpdate = MyEntityUpdateEnum.NONE;
            CleanupSegments(_cableSegments1);
            CleanupSegments(_cableSegments2);
        }

        // =====================================================================
        // INITIALISIERUNG
        // =====================================================================

        private bool TryGetSubparts()
        {
            if (!_winch.TryGetSubpart(RotorSubpartName, out _rotor))
                return false;
            if (!_winch.TryGetSubpart(Segment1SubpartName, out _segment1))
                return false;
            if (!_segment1.TryGetSubpart(Segment2SubpartName, out _segment2))
                return false;
            if (!_segment2.TryGetSubpart(Segment3SubpartName, out _segment3))
                return false;
            return true;
        }

        private bool TryGetDummies()
        {
            var baseDummies = new Dictionary<string, IMyModelDummy>();
            var tipDummies  = new Dictionary<string, IMyModelDummy>();

            _winch.Model.GetDummies(baseDummies);
            ((IMyModel)_segment3.Model).GetDummies(tipDummies);

            if (baseDummies.Count == 0 || tipDummies.Count == 0)
                return false;

            var pairs = new List<KeyValuePair<string, string>>();
            foreach (var kv in baseDummies)
            {
                if (kv.Key.ToLower().Contains(CableDummyName.ToLower()) && tipDummies.ContainsKey(kv.Key))
                    pairs.Add(new KeyValuePair<string, string>(kv.Key, kv.Key));
            }

            if (pairs.Count == 0)
                return false;

            if (pairs.Count == 1)
            {
                _cableStart1 = baseDummies[CableDummyName + "_1"];
                _cableEnd1   = tipDummies [CableDummyName + "_1"];
                _drawMode    = 1;
            }
            else if (pairs.Count == 2)
            {
                _cableStart1 = baseDummies[CableDummyName + "_1"];
                _cableEnd1   = tipDummies [CableDummyName + "_1"];
                _cableStart2 = baseDummies[CableDummyName + "_2"];
                _cableEnd2   = tipDummies [CableDummyName + "_2"];
                _drawMode    = 2;
            }
            else
            {
                _linePairs = new List<KeyValuePair<IMyModelDummy, IMyModelDummy>>();
                foreach (var pair in pairs)
                {
                    if (baseDummies.ContainsKey(pair.Key) && tipDummies.ContainsKey(pair.Value))
                        _linePairs.Add(new KeyValuePair<IMyModelDummy, IMyModelDummy>(
                            baseDummies[pair.Key], tipDummies[pair.Value]));
                }
                _drawMode = 0;
            }

            return true;
        }

        // =====================================================================
        // KABEL ZEICHNEN
        // =====================================================================

        private void DrawCables()
        {
            switch (_drawMode)
            {
                case 0:
                    DrawLineFallback();
                    break;
                case 1:
                    UpdateCableSegments(_cableStart1, _cableEnd1, ref _cableSegments1, ref _freezePos1);
                    break;
                case 2:
                    UpdateCableSegments(_cableStart1, _cableEnd1, ref _cableSegments1, ref _freezePos1);
                    UpdateCableSegments(_cableStart2, _cableEnd2, ref _cableSegments2, ref _freezePos2);
                    break;
            }
        }

        private void DrawLineFallback()
        {
            if (_linePairs == null || _segment3 == null)
                return;

            foreach (var pair in _linePairs)
            {
                Vector3D start = Vector3D.Transform(pair.Key.Matrix.Translation,   _winch.WorldMatrix);
                Vector3D end   = Vector3D.Transform(pair.Value.Matrix.Translation, _segment3.WorldMatrix);
                MySimpleObjectDraw.DrawLine(start, end, _ropeId, ref _ropeColor, 0.05f, BlendTypeEnum.PostPP);
            }
        }

        private void UpdateCableSegments(
            IMyModelDummy dummyStart, IMyModelDummy dummyEnd,
            ref List<MyEntity> segments, ref Vector3D freezePos)
        {
            if (dummyStart == null || dummyEnd == null || _segment3 == null)
                return;

            if (_winch.MarkedForClose || _winch.Closed)
                return;

            Vector3D posStart = Vector3D.Transform(dummyStart.Matrix.Translation, _winch.WorldMatrix);
            Vector3D posEnd   = Vector3D.Transform(dummyEnd.Matrix.Translation,   _segment3.WorldMatrix);

            freezePos = Vector3D.Transform(posEnd, _winch.PositionComp.WorldMatrixInvScaled);

            int targetCount = MathHelper.RoundToInt((float)(posEnd - posStart).Length() / _segmentLength);

            if (targetCount == 0)
                return;

            if (segments.Count < targetCount)
            {
                int toAdd = targetCount - segments.Count;
                for (int i = 0; i < toAdd; i++)
                {
                    double offset = segments.Count * _segmentLength;
                    var ent = SpawnCableSegment();
                    ent.Teleport(MatrixD.CreateWorld(
                        posEnd + _winch.WorldMatrix.Down * offset,
                        _winch.WorldMatrix.Forward,
                        _winch.WorldMatrix.Up));
                    segments.Add(ent);
                }
            }
            else if (segments.Count > targetCount)
            {
                var last = segments[segments.Count - 1];
                segments.RemoveAt(segments.Count - 1);
                last.Close();
            }
            else
            {
                double offset = 0;
                foreach (var seg in segments)
                {
                    offset += _segmentLength;
                    seg.Teleport(MatrixD.CreateWorld(
                        posEnd + _winch.WorldMatrix.Down * offset,
                        _winch.WorldMatrix.Forward,
                        _winch.WorldMatrix.Up));
                }
            }
        }

        private void FreezeCables()
        {
            switch (_drawMode)
            {
                case 1:
                    FreezeSegments(_freezePos1, _cableSegments1);
                    break;
                case 2:
                    FreezeSegments(_freezePos1, _cableSegments1);
                    FreezeSegments(_freezePos2, _cableSegments2);
                    break;
            }
        }

        private void FreezeSegments(Vector3D localPos, List<MyEntity> segments)
        {
            if (localPos == Vector3D.Zero || segments.Count == 0)
                return;

            Vector3D worldPos = Vector3D.Transform(localPos, _winch.WorldMatrix);
            double offset = 0;
            foreach (var seg in segments)
            {
                offset += _segmentLength;
                seg.Teleport(MatrixD.CreateWorld(
                    worldPos + _winch.WorldMatrix.Down * offset,
                    _winch.WorldMatrix.Forward,
                    _winch.WorldMatrix.Up));
            }
        }

        // =====================================================================
        // ROTOR ANIMATION
        // =====================================================================

        private void AnimateRotor()
        {
            if (_rotor == null || _rotor.Closed)
                return;

            if (_winch.CurrentPosition == _lastPistonPos)
                return;

            _lastPistonPos = _winch.CurrentPosition;
            float delta = _winch.Velocity / 20f;

            var hingePos  = _rotor.PositionComp.LocalMatrixRef.Translation;
            var toOrigin  = Matrix.CreateTranslation(-hingePos);
            var toHinge   = Matrix.CreateTranslation(hingePos);
            var rotation  = Matrix.CreateRotationZ(delta);

            var localMatrix = _rotor.PositionComp.LocalMatrix;
            localMatrix *= toOrigin * rotation * toHinge;
            _rotor.PositionComp.LocalMatrix = localMatrix;
        }

        // =====================================================================
        // HILFSFUNKTIONEN
        // =====================================================================

        private MyEntity SpawnCableSegment()
        {
            var ent = new MyEntity();
            ent.Init(null, _cableModel, null, null, null);
            ent.Render.CastShadows = true;
            ent.IsPreview          = true;
            ent.Save               = false;
            ent.SyncFlag           = false;
            ent.NeedsWorldMatrix   = false;
            ent.Flags |= EntityFlags.IsNotGamePrunningStructureObject;
            MyEntities.Add(ent, true);
            return ent;
        }

        private void CleanupSegments(List<MyEntity> segments)
        {
            foreach (var seg in segments)
            {
                if (seg != null)
                    seg.Close();
            }
            segments.Clear();
        }
    }
}