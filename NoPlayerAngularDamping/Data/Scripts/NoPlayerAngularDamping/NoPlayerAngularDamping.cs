using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI;
using Sandbox.Common.ObjectBuilders;
using VRage.Game.ModAPI;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace JamacSpaceGameMod
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class NoPlayerAngularDamping : MySessionComponentBase
    {
        
        private HashSet<IMyEntity> characterEntitiesSet = new HashSet<IMyEntity>();
        private List<IMyEntity> characterEntitiesList = new List<IMyEntity>();
        private Vector3D lastAngularVelocity;
        private Vector3D lastSupportNormal;
        private bool supported = false;

        public override void BeforeStart()
        {
            // Run for all existing characters
            var entities = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(entities);
            foreach(IMyEntity entity in entities)
            {
                OnEntityAdd(entity);
            }
            
            // Register callback for not-yet-existing characters
            MyAPIGateway.Entities.OnEntityAdd += OnEntityAdd;
            MyAPIGateway.Entities.OnEntityRemove += OnEntityRemove;

            lastAngularVelocity = Vector3D.Zero;
            lastSupportNormal = Vector3D.Zero;

            MyLog.Default.WriteLineAndConsole("NoPlayerAngularDamping: Setup complete");
        }

        public override void UpdateBeforeSimulation()
        {
            foreach(IMyEntity entity in characterEntitiesList)
            {
                if(entity != null && entity.Physics != null)
                {
                    try
                    {
                        float dt = 0.01666667f;

                        IMyCharacter character = (IMyCharacter) entity;

                        // check if current movement state is one where player *could* be standing on a surface
                        if(!(character.CurrentMovementState == MyCharacterMovementEnum.Flying
                        || character.CurrentMovementState == MyCharacterMovementEnum.Jump
                        || character.CurrentMovementState == MyCharacterMovementEnum.Falling
                        || character.CurrentMovementState == MyCharacterMovementEnum.Sitting
                        || character.CurrentMovementState == MyCharacterMovementEnum.Died
                        || character.CurrentMovementState == MyCharacterMovementEnum.Ladder
                        || character.CurrentMovementState == MyCharacterMovementEnum.LadderUp
                        || character.CurrentMovementState == MyCharacterMovementEnum.LadderDown
                        || character.CurrentMovementState == MyCharacterMovementEnum.LadderOut
                        ))
                        {
                            if(!supported && (entity.Physics.SupportNormal - lastSupportNormal).Length() > 0.000000000001)
                            {
                                supported = true;
                            }
                        }
                        else
                        {
                            supported = false;
                        }

                        if(!supported &&
                        !( character.CurrentMovementState == MyCharacterMovementEnum.Flying
                        || character.CurrentMovementState == MyCharacterMovementEnum.Sitting
                        || character.CurrentMovementState == MyCharacterMovementEnum.Died
                        || character.CurrentMovementState == MyCharacterMovementEnum.Ladder
                        || character.CurrentMovementState == MyCharacterMovementEnum.LadderUp
                        || character.CurrentMovementState == MyCharacterMovementEnum.LadderDown
                        || character.CurrentMovementState == MyCharacterMovementEnum.LadderOut
                        ))
                        {
                            // apply rotation
                            Vector3D angularNormalized = Vector3D.Normalize(lastAngularVelocity);
                            if(!Double.IsNaN(angularNormalized.X))
                            {
                                Quaternion rotation = Quaternion.CreateFromRotationMatrix(MatrixD.CreateFromAxisAngle(angularNormalized, lastAngularVelocity.Length() * dt));
                                MatrixD worldMatrix = entity.PositionComp.WorldMatrixRef;
                                MatrixD newWorldMatrix = MatrixD.Transform(worldMatrix.GetOrientation(), rotation);
                                newWorldMatrix.Translation = worldMatrix.Translation;
                                entity.PositionComp.SetWorldMatrix(newWorldMatrix);
                            }
                        }
                        else
                        {
                            lastAngularVelocity = entity.Physics.AngularVelocity;
                        }

                        lastSupportNormal = entity.Physics.SupportNormal;
                    }
                    catch(Exception e)
                    {
                        MyLog.Default.WriteLineAndConsole($"Error in NoPlayerAngularDamping:\n{e}");
                    }
                }
            }
        }

        public void OnEntityAdd(IMyEntity entity)
        {
            if(entity != null && entity is IMyCharacter)
            {
                characterEntitiesSet.Add(entity);
                characterEntitiesList.Add(entity);
            }
        }

        public void OnEntityRemove(IMyEntity entity)
        {
            if(entity != null && characterEntitiesSet.Contains(entity))
            {
                characterEntitiesSet.Remove(entity);
                characterEntitiesList.Remove(entity);
            }
        }
    }
}