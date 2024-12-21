using System.Collections.Generic;
using UnityEngine;

namespace SPHWater.Assets.Scripts.Phy._2D
{
    public class Simulation2DTest : MonoBehaviour
    {
        public int numParticles = 1000;
        public float gravity = -9.81f;
        public float smoothingRadius = 2.0f;
        public float targetDensity = 1000f;
        public float pressureMultiplier = 3f;
        public float viscosityStrength = 0.1f;
        public Vector2 boundsSize = new Vector2(10f, 10f);
        public Vector2 obstacleSize = new Vector2(1f, 1f);
        public Vector2 obstacleCentre = new Vector2(0f, 0f);
        public float deltaTime = 0.02f;

        // Particle data
        private List<Particle> particles = new List<Particle>();

        // Settings
        private Vector2 interactionInputPoint;
        private float interactionInputStrength;
        private float interactionInputRadius;

        void Start()
        {
            // Initialize particles with random positions
            for (int i = 0; i < numParticles; i++)
            {
                Particle p = new Particle
                {
                    Position = new Vector2(Random.Range(-5f, 5f), Random.Range(-5f, 5f)),
                    Velocity = Vector2.zero,
                    Density = 0f,
                    NearDensity = 0f
                };
                particles.Add(p);
            }
        }

        void FixedUpdate()
        {
            // Run the simulation every frame
            RunSimulation();
        }

        void RunSimulation()
        {
            // 1. External Forces (gravity, interaction)
            ApplyExternalForces();

            // 2. Calculate Densities and Near Densities
            CalculateDensities();

            // 3. Pressure and Viscosity
            CalculatePressureForce();
            CalculateViscosity();

            // 4. Update Positions
            UpdatePositions();
        }

        void ApplyExternalForces()
        {
            foreach (var particle in particles)
            {
                Vector2 gravityForce = new Vector2(0, gravity);
                particle.Velocity += gravityForce * deltaTime;

                // Interaction input (Mouse or other forces)
                if (interactionInputStrength != 0)
                {
                    Vector2 inputDirection = interactionInputPoint - particle.Position;
                    float distance = inputDirection.magnitude;

                    if (distance < interactionInputRadius)
                    {
                        float strength = interactionInputStrength * (1 - (distance / interactionInputRadius));
                        particle.Velocity += inputDirection.normalized * strength * deltaTime;
                    }
                }
            }
        }

        void CalculateDensities()
        {
            foreach (var particle in particles)
            {
                particle.Density = 0f;
                particle.NearDensity = 0f;

                foreach (var otherParticle in particles)
                {
                    if (particle == otherParticle) continue;

                    Vector2 offset = otherParticle.Position - particle.Position;
                    float distanceSquared = offset.sqrMagnitude;

                    if (distanceSquared < smoothingRadius * smoothingRadius)
                    {
                        float distance = Mathf.Sqrt(distanceSquared);
                        particle.Density += SpikyKernel(distance);
                        particle.NearDensity += SpikyKernel(distance);
                    }
                }
            }
        }

        float SpikyKernel(float distance)
        {
            float h = smoothingRadius;
            float q = distance / h;

            if (q < 1.0f)
            {
                return 15.0f / (Mathf.PI * Mathf.Pow(h, 6)) * Mathf.Pow(1 - q, 3);
            }
            return 0f;
        }

        void CalculatePressureForce()
        {
            foreach (var particle in particles)
            {
                Vector2 pressureForce = Vector2.zero;

                foreach (var otherParticle in particles)
                {
                    if (particle == otherParticle) continue;

                    Vector2 offset = otherParticle.Position - particle.Position;
                    float distance = offset.magnitude;

                    if (distance < smoothingRadius)
                    {
                        float pressure = (particle.Density - targetDensity) * pressureMultiplier;
                        float neighborPressure = (otherParticle.Density - targetDensity) * pressureMultiplier;

                        float forceMagnitude = (pressure + neighborPressure) * Mathf.Pow(1 - (distance / smoothingRadius), 2);
                        pressureForce += offset.normalized * forceMagnitude;
                    }
                }

                particle.Velocity += pressureForce * deltaTime;
            }
        }

        void CalculateViscosity()
        {
            foreach (var particle in particles)
            {
                Vector2 viscosityForce = Vector2.zero;

                foreach (var otherParticle in particles)
                {
                    if (particle == otherParticle) continue;

                    Vector2 offset = otherParticle.Position - particle.Position;
                    float distance = offset.magnitude;

                    if (distance < smoothingRadius)
                    {
                        viscosityForce += (otherParticle.Velocity - particle.Velocity) * Mathf.Pow(1 - (distance / smoothingRadius), 2);
                    }
                }

                particle.Velocity += viscosityForce * viscosityStrength * deltaTime;
            }
        }

        void UpdatePositions()
        {
            foreach (var particle in particles)
            {
                particle.Position += particle.Velocity * deltaTime;

                // Collision detection with bounds
                if (particle.Position.x < -boundsSize.x / 2 || particle.Position.x > boundsSize.x / 2)
                {
                    particle.Velocity.x *= -0.95f;
                    particle.Position.x = Mathf.Clamp(particle.Position.x, -boundsSize.x / 2, boundsSize.x / 2);
                }

                if (particle.Position.y < -boundsSize.y / 2 || particle.Position.y > boundsSize.y / 2)
                {
                    particle.Velocity.y *= -0.95f;
                    particle.Position.y = Mathf.Clamp(particle.Position.y, -boundsSize.y / 2, boundsSize.y / 2);
                }

                // Handle obstacles (simple square bounds)
                if (Mathf.Abs(particle.Position.x - obstacleCentre.x) < obstacleSize.x / 2 &&
                    Mathf.Abs(particle.Position.y - obstacleCentre.y) < obstacleSize.y / 2)
                {
                    particle.Velocity *= -0.95f;
                }
            }
        }

        public void SetInteractionInput(Vector2 point, float strength, float radius)
        {
            interactionInputPoint = point;
            interactionInputStrength = strength;
            interactionInputRadius = radius;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            foreach (var particle in particles)
            {
                Gizmos.DrawSphere(particle.Position, 0.1f);
            }

            // Draw bounding box
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(Vector3.zero, boundsSize);
        }

    }

    public class Particle
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public Vector2 PredictedPosition;
        public float Density;
        public float NearDensity;
    }
}