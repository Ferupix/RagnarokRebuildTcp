using Assets.Scripts.Network;
using UnityEngine;
using Random = UnityEngine.Random;
using Assets.Scripts.Effects.PrimitiveData;
using UnityEngine.AddressableAssets;

namespace Assets.Scripts.Effects.EffectHandlers.Skills._ToS.Cleric
{
    [RoEffect("HealCleric")]
    public class HealClericEffect : IEffectHandler
    {
        public static Ragnarok3dEffect Create(ServerControllable target)
        {
            var effect = RagnarokEffectPool.Get3dEffect(EffectType.HealCleric);
            effect.FollowTarget = target.gameObject;
            effect.SourceEntity = target;
            effect.SetDurationByTime(30f);
            effect.UpdateOnlyOnFrameChange = true;
            effect.DestroyOnTargetLost = true;
            effect.Flags[0] = 0;

            // Square Aura Section
            Material mat = EffectSharedMaterialManager.GetMaterial(EffectMaterialType.MagnusExorcismus);

            var prim = effect.LaunchPrimitive(PrimitiveType.RectUp, mat, 9999999f);
            prim.CreateParts(1);

            var part = prim.Parts[0];

            part.RiseAngle = Random.Range(0, 360f);
            part.Alpha = 18f;
            part.Heights[0] = 0.5f; //x radius
            part.Heights[1] = 0.5f; //y radius
            part.Heights[2] = 0f;
            part.MaxHeight = 5.2f; //3.2f; //10f;

            // Square Ground Texture Section
            var primGroundSquare = effect.LaunchPrimitive(PrimitiveType.Texture2D, EffectSharedMaterialManager.GetMaterial(EffectMaterialType.SkillSpriteAlphaAdditiveNoZCheck), 9999999f);
            primGroundSquare.Material.shader = Shader.Find("Ragnarok/AdditiveShaderPulse");

            var data = primGroundSquare.GetPrimitiveData<Texture2DData>();

            var task = Addressables.LoadAssetAsync<Sprite>("Assets/Effects/Textures/_ToS/Cleric/pattern034.tga");
            task.Completed += ah =>
            {
                data.Sprite = ah.Result;
            };

            data.Color = Color.magenta;
            data.Alpha = 192;
            data.AlphaSpeed = 0;
            data.Size = Vector2.one;
            data.MinSize = Vector2.zero;
            data.MaxSize = Vector2.one;
            data.FadeOutLength = 0f;
            data.Speed = Vector3.zero;
            data.Acceleration = Vector2.zero;

            primGroundSquare.transform.localPosition = new Vector3(0f, 0f, 0f);
            primGroundSquare.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            primGroundSquare.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);

            return effect;
        }

        public bool Update(Ragnarok3dEffect effect, float pos, int step)
        {
            if (step == 0)
            {
                // Ground Swirl Section
                var primGroundSwirl = effect.LaunchPrimitive(PrimitiveType.Texture2D, EffectSharedMaterialManager.GetMaterial(EffectMaterialType.SkillFlashEffect), 9999999f);
                //primGroundSwirl.Material.shader = Shader.Find("Ragnarok/AdditiveShaderPulse");

                var dataSwirl = primGroundSwirl.GetPrimitiveData<Texture2DData>();

                var taskSwirl = Addressables.LoadAssetAsync<Sprite>("Assets/Effects/Textures/_ToS/Cleric/circle079.tga");
                taskSwirl.Completed += ah =>
                {
                    dataSwirl.Sprite = ah.Result;
                };

                dataSwirl.Color = Color.magenta;
                dataSwirl.Alpha = 64;
                dataSwirl.AlphaSpeed = 0;
                dataSwirl.Size = Vector2.one; //Vector2.zero;
                dataSwirl.MinSize = Vector2.zero;
                dataSwirl.MaxSize = Vector2.one;//new Vector2(9999, 9999);
                dataSwirl.FadeOutLength = 2f;
                dataSwirl.Speed = Vector2.zero;
                dataSwirl.Acceleration = Vector2.zero;

                primGroundSwirl.transform.localPosition = new Vector3(0f, 0.01f, 0f);
                primGroundSwirl.transform.localRotation = Quaternion.Euler(90f, 0, 0);
                primGroundSwirl.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
            }

            if (effect.Primitives.Count == 0)
                return false;

            for (var i = 0; i < effect.Primitives.Count; i++)
            {
                var prim = effect.Primitives[i];

                if (i == 2 && step % 5 == 0)
                {
                    // Rotate Swirl Texture
                    Quaternion targetRot = Quaternion.Euler(90f, step * 3, 0f);
                    prim.transform.localRotation = Quaternion.Lerp(prim.transform.localRotation, targetRot, Time.deltaTime * step * 480f);

                    if(step == 120) // Stop swirl after 2 seconds (120 frames)
                        prim.EndPrimitive();
                }

                if (effect.Flags[0] == 0 && !effect.FollowTarget)
                {
                    prim.FrameDuration = 0;
                    effect.Flags[0] = 1;
                }
            }

            return effect.IsTimerActive;
            //return prim.IsActive;
        }
    }
}
