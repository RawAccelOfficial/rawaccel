namespace RawAccel.Contracts
{
    // 2D vector container used for cap, domainXY, rangeXY in the JSON
    // contract. Matches the layout produced by wrapper/wrapper.cpp Vec2<T>.
    public struct Vec2<T>
    {
        public T x;
        public T y;
    }
}
