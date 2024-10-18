
using System.Collections.Generic;

namespace NopTop.Plugin.Payments.Zarinpal.Models;
public class RestRequestModel
{
    public RestRequestData data { get; set; }
    public List<string> errors { get; set; }
}
public class RestRequestData
{
    public int code { get; set; }
    public string message { get; set; }
    public string authority { get; set; }
    public string fee_type { get; set; }
    public int fee { get; set; }
}
