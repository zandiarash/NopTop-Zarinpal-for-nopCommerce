using System.ComponentModel.DataAnnotations;

namespace NopTop.Plugin.Payments.Zarinpal.Models;
public enum EnumZarinGate
{
    //Automatically choose the best
    [Display(Name = "Zarin Gate")]
    ZarinGate = 0,
    //Persian Switch
    [Display(Name = "Asan Pardakht")]
    Asan = 1,
    //Saman
    [Display(Name = "Saman")]
    Sep = 2,
    //Sadad
    [Display(Name = "Sadad")]
    Sad = 3,
    //Parsian
    [Display(Name = "Parsian")]
    Pec = 4,
    //FanavaCart
    [Display(Name = "FanavaCart")]
    Fan = 5,
    //Emtiaz
    [Display(Name = "Emtiaz")]
    Emz = 6,
}