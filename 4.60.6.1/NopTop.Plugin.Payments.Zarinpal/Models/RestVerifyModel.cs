
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NopTop.Plugin.Payments.Zarinpal.Models;

public class RestVerifyModel<T>
{
    public RestVerifyData data { get; set; }
    public T errors { get; set; }
}
public class RestVerifyData
{
    public int code { get; set; }
    public string message { get; set; }
    public string card_hash { get; set; }
    public string card_pan { get; set; }
    public long ref_id { get; set; }
    public string fee_type { get; set; }
    public int fee { get; set; }
}
public class ErrorDetails
{
    public string message { get; set; }
    public int code { get; set; }
    public List<string> validations { get; set; }
}
