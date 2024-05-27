using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets
{
    // Przechowuje informację o sprężynach
    public class SpringsHolder
    {
        public Point[] pointsFirst;
        public Point[] pointsSeconds;
        public Spring[] springs;

        public bool differentPointsReference => pointsFirst == pointsSeconds;
        public int numberOfSprings => springs.Length;
        public int numberOfPoints =>
            differentPointsReference ? 
                pointsFirst.Length :
                pointsFirst.Length + pointsSeconds.Length; 

        public SpringsHolder(Point[] pointsFirst, Point[] pointsSeconds, Spring[] spring ) 
        {
            this.pointsFirst = pointsFirst;
            this.pointsSeconds = pointsSeconds;
            this.springs = spring;
        }

        public void UpdateSpringForce()
        {
            for (int i = 0; i < numberOfSprings; i++)
            {
                var spring = springs[i];

                Vector3 springDirection = pointsSeconds[spring.second].position - pointsFirst[spring.first].position;

                float force = spring.elasticityScalar * (spring.l_0 - springDirection.magnitude);
                springDirection = springDirection.normalized;

                pointsFirst[spring.first].springForce -= springDirection * force;
                pointsSeconds[spring.second].springForce += springDirection * force;
            }
        }
    }
}
