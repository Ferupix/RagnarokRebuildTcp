using Assets.Scripts.Objects;
using UnityEngine;
using Assets.Scripts.Network;
using Assets.Scripts.Sprites;
using Assets.Scripts.Effects.PrimitiveData;

namespace Assets.Scripts.Effects.EffectHandlers.Skills._ToS.Dievdirbys
{
    [RoEffect("StatueOfGoddess")]
    public class StatueOfGoddessEffect : IEffectHandler
    {
        public static Ragnarok3dEffect LaunchStatueOfGoddess(GameObject target)
        {
            ServerControllable src = target.GetComponent<ServerControllable>();
            //src.AttachEffect(CastEffect.Create(1f, target, AttackElement.Holy));
            
            var mat = EffectSharedMaterialManager.GetMaterial(EffectMaterialType.MapPillarBlue);
            
            var effect = RagnarokEffectPool.Get3dEffect(EffectType.StatueOfGoddess);
            effect.SetDurationByFrames(180 * 60);
            effect.FollowTarget = target;
            effect.DestroyOnTargetLost = false;
            effect.transform.position = target.transform.position;
            effect.transform.localScale = new Vector3(1, 1, 1);
            //effect.UpdateOnlyOnFrameChange = true; // ToDo: Test more
            effect.Flags[0] = 0;
            float scale = 6f;
            
            AudioManager.Instance.OneShotSoundEffect(-1, "ef_glasswall.ogg", effect.transform.position, 0.8f); // Safetywall sound
            
            // Blue Pillar Section
            var prim = effect.LaunchPrimitive(PrimitiveType.Heal, mat, 60f);
            prim.CreateParts(4);

            prim.Parts[0] = new EffectPart()
            {
                Active = true,
                Step = 0,
                AlphaRate = 2,
                AlphaTime = 3600,
                CoverAngle = 360,
                MaxHeight = 34 * scale,
                Angle = Random.Range(0f, 360f),
                Distance = 3.6f * scale,
                RiseAngle = 90
            };

            prim.Parts[1] = new EffectPart()
            {
                Active = true,
                Step = 0,
                AlphaRate = 2,
                AlphaTime = 3600,
                CoverAngle = 360,
                MaxHeight = 37 * scale,
                Angle = Random.Range(0f, 360f),
                Distance = 3.3f * scale,
                RiseAngle = 90
            };

            prim.Parts[2] = new EffectPart()
            {
                Active = true,
                Step = 0,
                AlphaRate = 2,
                AlphaTime = 3600,
                CoverAngle = 360,
                MaxHeight = 40 * scale,
                Angle = Random.Range(0f, 360f),
                Distance = 3f * scale,
                RiseAngle = 90
            };

            prim.Parts[3] = new EffectPart()
            {
                Active = false,
            };

            for (var i = 0; i < 4; i++)
            {
                for (var j = 0; j < EffectPart.SegmentCount; j++)
                {
                    prim.Parts[i].Heights[j] = 0;
                    prim.Parts[i].Flags[j] = 0; //0 is no spin, 1 is spin
                }
            }

            return effect;
        }

        public bool Update(Ragnarok3dEffect effect, float pos, int step)
        {
            if (effect.Flags[0] > 0)
            {
                foreach (var p in effect.Primitives)
                    if (p.IsActive)
                        return step < effect.DurationFrames;

                return false;
            }

            if (effect.FollowTarget == null)
            {
                effect.Flags[0] = 1;
                foreach (var p in effect.Primitives)
                {
                    for (var i = 0; i < p.PartsCount; i++)
                        p.Parts[i].AlphaTime = p.Step;
                }
            }

            // Floating Particles Section
            if (step < (60 * 180) && step % 45 == 0)
            {
                var duration = 150 / 60f;
                var size = Random.Range(0.05f, 0.15f);

                var primParticles = effect.LaunchPrimitive(PrimitiveType.Texture3D, EffectSharedMaterialManager.GetMaterial(EffectMaterialType.ParticleAdditive), duration);
                primParticles.SetBillboardMode(BillboardStyle.Normal);
                var data = primParticles.GetPrimitiveData<Texture3DData>();
                float scale = 2f;
                var offset = VectorHelper.RandomPositionInCylinder(2f * scale, 6f * scale) / 5f;

                data.Sprite = EffectSharedMaterialManager.GetParticleSprite("particle6"); // Star particle
                data.ScalingSpeed = Vector3.zero;
                data.Alpha = 0;

                data.AlphaMax = 250f;
                data.AlphaSpeed = data.AlphaMax / 10 * 60; //10 frames
                data.Size = new Vector2(size, size);
                data.MinSize = data.Size;
                data.MaxSize = data.Size;
                data.IsStandingQuad = true;
                data.FadeOutTime = 45 / 60f;
                data.Color = Color.white;
                data.AngleSpeed = 3f * 60f * Mathf.Deg2Rad;
                primParticles.Velocity = new Vector3(0f, Random.Range(0.4f, 0.7f) / 5f * 60f, 0f);
                primParticles.Acceleration = -(primParticles.Velocity / duration);
                primParticles.transform.localPosition = offset;
            }

            return step < effect.DurationFrames;
        }
    }
}
