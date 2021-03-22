﻿namespace Revit.TestRunner.Shared.Communication.Dto
{
    /// <summary>
    /// Simple Get call Dto. No Parameters.
    /// </summary>
    public class NoParameterDto : BaseRequestDto
    {
        public NoParameterDto() : base( DtoType.NoParameterDto )
        {
        }
    }
}