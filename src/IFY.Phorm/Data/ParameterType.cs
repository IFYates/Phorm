namespace IFY.Phorm.Data
{
    public enum ParameterType
    {
        // Matching values for System.Data.ParameterDirection
        Input = 1,
        Output = 2,
        InputOutput = 3,
        ReturnValue = 6,

        /// <summary>
        /// Concatenated output of any printed data
        /// </summary>
        Console = 100,
    }
}
