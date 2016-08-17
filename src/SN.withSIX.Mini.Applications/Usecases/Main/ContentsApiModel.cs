using System.Runtime.Serialization;
using withSIX.Api.Models;

namespace SN.withSIX.Mini.Applications.Usecases.Main
{
    [DataContract]
    public abstract class ContentsApiModel : PageModel<ContentApiModel> {}
}