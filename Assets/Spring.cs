

namespace Assets
{
    public struct Spring
    {
        public int first;
        public int second;

        public float elasticityScalar;
        public float l_0;


        public Spring(int first, int second)
        {
            this.first = first;
            this.second = second;
            elasticityScalar = 0.1f;
            l_0 = 3;
        }


        public void Set(int f, int s, float elasticityScalar, float l_0)
        {
            first = f;
            second = s;
            this.elasticityScalar = elasticityScalar;
            this.l_0 = l_0;
        }


    }
}
