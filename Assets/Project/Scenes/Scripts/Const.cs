using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Const : MonoBehaviour
{
    public const string NUM_STORY = "Num_Story";

    public const string EXP = "Exp";

    public const string GOLD = "Gold";

    public const string Stage = "Stage";

    public const string LEVEL = "Level";

    public const string CURRENTWEAPON = "CurrentWeapon";

    #region level     

    public const string Level0 = "Tứ Cửu";

    public const string LevelHongCon = "Hồng Côn";

    public const string LevelSHHC = "Song Hoa Hồng Côn";

    public const string LevelNhiLo = "Nhị Lộ Nguyên Soái";

    public const string LevelLongDau = "Long Đầu";



    public static string GetLevel(float exp, ref int nextlevel)
    {
        Debug.Log("exp: " + exp);
        string strReturn = string.Empty;
        if (exp <= 3)
        {
            nextlevel = 3;
            return Level0;
        }            
        else if (exp > 3 && exp <= 90)
        {
            nextlevel = 90;
            return LevelHongCon + " level: " + (int)exp;
        }            
        else if (exp > 90 && exp <= 270)
        {
            nextlevel = 270;
            return LevelSHHC + " level: " + (exp / 90);
        }            
        else if (exp > 270 && exp < 600)
        {
            nextlevel = 600;
            return LevelNhiLo + " level: " + (exp / 270);
        }            
        else
        {
            nextlevel = 0;
            return LevelLongDau + " level: " + (exp / 600);
        }            
    }

    public static string GetLevel(float exp)
    {
        Debug.Log("exp: " + exp);
        string strReturn = string.Empty;
        if (exp == 0)
        {
            return Level0;
        }
        else if (exp > 1 && exp <= 50)
        {
            return LevelHongCon + " level: " + (int)exp;
        }
        else if (exp > 50 && exp <= 200)
        {
            return LevelSHHC + " level: " + (exp / 50);
        }
        else if (exp > 200 && exp < 500)
        {
            return LevelNhiLo + " level: " + (exp / 200);
        }
        else
        {
            return LevelLongDau + " level: " + (exp / 500);
        }
    }

    public delegate void Callback();

    #endregion
}
