using UnityEngine;

namespace Assets
{
    public struct Point
    {
        public Vector3 position;
        public Vector3 velocity;
        public Vector3 acceleration;

        public Vector3 springForce;
        public float inversMass;
        public float mass { set { inversMass = 1 / value; } }

        public Point(Vector3 po, Vector3 v, Vector3 a, Vector3 springForce, float mass)
        {
            this.position = po;
            this.velocity = v;
            this.acceleration = a;
            inversMass = 1 / mass;
            this.springForce = springForce;
        }

        public void UpdatePositionAndDerivatives(float delta, float dampingScalar, float range, float u)
        {
            acceleration = (springForce - velocity * dampingScalar) * inversMass;
            position = position + delta * velocity ;
            velocity = velocity + delta * acceleration;
            velocity = Bouncing(u, range, delta, velocity);
        }

        private Vector3 Bouncing(float u, float range, float delta, Vector3 proposalVelocity, int numberOfRecurection = 0)
        {
            if (numberOfRecurection > 6)
            {
                return Vector3.zero;
            }

            Vector3 probosalPosition = position + delta * proposalVelocity;
            Vector3 nextProposalVelocity = proposalVelocity;

            if (Mathf.Abs(probosalPosition.x) > range)
                nextProposalVelocity.x = -u* nextProposalVelocity.x;

            if (Mathf.Abs(probosalPosition.y) > range)
                nextProposalVelocity.y = -u * nextProposalVelocity.y;

            if (Mathf.Abs(probosalPosition.z) > range)
                nextProposalVelocity.z = -u * nextProposalVelocity.z;

            if( nextProposalVelocity == proposalVelocity )
            {
                return nextProposalVelocity;
            }
            else
            {
                return Bouncing(u, range, delta, nextProposalVelocity, numberOfRecurection + 1);
            }
        }
    }
}
