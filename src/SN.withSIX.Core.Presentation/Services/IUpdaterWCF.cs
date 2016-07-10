// <copyright company="SIX Networks GmbH" file="IUpdaterWCF.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.ServiceModel;


namespace SN.withSIX.Core.Presentation.Services
{
    [ServiceContract]
    
    public interface IUpdaterWCF
    {
        [OperationContract]
        int PerformOperation(params string[] args);

        [OperationContract]
        int LaunchGame(params string[] args);
    }
}