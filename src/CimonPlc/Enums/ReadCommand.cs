namespace CimonPlc.Enums
{
    public enum ReadCommand
    {
        /// <summary>
        /// Reads data from memory by the word.
        /// </summary>
        WordBlockRead = 0x52,
        /// <summary>
        /// Reads data from memory by the bit.
        /// </summary>
        BitBlockRead = 0x72
    }
}
