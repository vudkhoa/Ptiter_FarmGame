namespace Core.Module.Input
{
    public readonly struct PointerButtonDownPayload
    {
        public readonly int Button;

        public PointerButtonDownPayload(int button)
        {
            Button = button;
        }
    }
}