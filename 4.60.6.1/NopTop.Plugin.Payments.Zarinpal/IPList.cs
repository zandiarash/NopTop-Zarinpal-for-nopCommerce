// Copyright by Bo Norgaard, All rights reserved.
//https://www.codeproject.com/Articles/2553/IP-list-Check-an-IP-number-against-a-list-in-C
//http://www.opensource.org/licenses/bsd-license.php
//The code is copyrighted by Bo Norgaard, but using the code is free, it requires no license and no royalty charge applies.

using System;
using System.Text;
using System.Collections;


namespace NopTop.Plugin.Payments.Zarinpal;
/// <summary>
/// Internal class for storing a range of IP numbers with the same IP mask
/// </summary>
internal class IPArrayList
{
    private bool _isSorted = false;
    private ArrayList _ipNumList = new ArrayList();
    private uint _ipmask;

    /// <summary>
    /// Constructor that sets the mask for the list
    /// </summary>
    public IPArrayList(uint mask)
    {
        _ipmask = mask;
    }

    /// <summary>
    /// Add a new IP numer (range) to the list
    /// </summary>
    public void Add(uint iPNum)
    {
        _isSorted = false;
        _ipNumList.Add(iPNum & _ipmask);
    }

    /// <summary>
    /// Checks if an IP number is within the ranges included by the list
    /// </summary>
    public bool Check(uint iPNum)
    {
        bool found = false;
        if (_ipNumList.Count > 0)
        {
            if (!_isSorted)
            {
                _ipNumList.Sort();
                _isSorted = true;
            }
            iPNum = iPNum & _ipmask;
            if (_ipNumList.BinarySearch(iPNum) >= 0)
                found = true;
        }
        return found;
    }

    /// <summary>
    /// Clears the list
    /// </summary>
    public void Clear()
    {
        _ipNumList.Clear();
        _isSorted = false;
    }

    /// <summary>
    /// The ToString is overriden to generate a list of the IP numbers
    /// </summary>
    public override string ToString()
    {
        StringBuilder buf = new StringBuilder();
        foreach (uint ipnum in _ipNumList)
        {
            if (buf.Length > 0)
                buf.Append("\r\n");
            buf.Append(((int)ipnum & 0xFF000000) >> 24).Append('.');
            buf.Append(((int)ipnum & 0x00FF0000) >> 16).Append('.');
            buf.Append(((int)ipnum & 0x0000FF00) >> 8).Append('.');
            buf.Append(((int)ipnum & 0x000000FF));
        }
        return buf.ToString();
    }

    /// <summary>
    /// The IP mask for this list of IP numbers
    /// </summary>
    public uint Mask
    {
        get
        {
            return _ipmask;
        }
    }
}

/// <summary>
/// Summary description for Class1.
/// </summary>
public class IPList
{
    private ArrayList _ipRangeList = new ArrayList();
    private SortedList _maskList = new SortedList();
    private ArrayList _usedList = new ArrayList();

    public IPList()
    {
        // Initialize IP mask list and create IPArrayList into the ipRangeList
        uint mask = 0x00000000;
        for (int level = 1; level < 33; level++)
        {
            mask = (mask >> 1) | 0x80000000;
            _maskList.Add(mask, level);
            _ipRangeList.Add(new IPArrayList(mask));
        }
    }

    // Parse a String IP address to a 32 bit unsigned integer
    // We can't use System.Net.IPAddress as it will not parse
    // our masks correctly eg. 255.255.0.0 is pased as 65535 !
    private uint parseIP(string iPNumber)
    {
        uint res = 0;
        string[] elements = iPNumber.Split(new Char[] { '.' });
        if (elements.Length == 4)
        {
            res = (uint)Convert.ToInt32(elements[0]) << 24;
            res += (uint)Convert.ToInt32(elements[1]) << 16;
            res += (uint)Convert.ToInt32(elements[2]) << 8;
            res += (uint)Convert.ToInt32(elements[3]);
        }
        return res;
    }

    /// <summary>
    /// Add a single IP number to the list as a string, ex. 10.1.1.1
    /// </summary>
    public void Add(string ipNumber)
    {
        this.Add(parseIP(ipNumber));
    }

    /// <summary>
    /// Add a single IP number to the list as a unsigned integer, ex. 0x0A010101
    /// </summary>
    public void Add(uint ip)
    {
        ((IPArrayList)_ipRangeList[31]).Add(ip);
        if (!_usedList.Contains((int)31))
        {
            _usedList.Add((int)31);
            _usedList.Sort();
        }
    }

    /// <summary>
    /// Adds IP numbers using a mask for range where the mask specifies the number of
    /// fixed bits, ex. 172.16.0.0 255.255.0.0 will add 172.16.0.0 - 172.16.255.255
    /// </summary>
    public void Add(string ipNumber, string mask)
    {
        this.Add(parseIP(ipNumber), parseIP(mask));
    }

    /// <summary>
    /// Adds IP numbers using a mask for range where the mask specifies the number of
    /// fixed bits, ex. 0xAC1000 0xFFFF0000 will add 172.16.0.0 - 172.16.255.255
    /// </summary>
    public void Add(uint ip, uint umask)
    {
        object level = _maskList[umask];
        if (level != null)
        {
            ip = ip & umask;
            ((IPArrayList)_ipRangeList[(int)level - 1]).Add(ip);
            if (!_usedList.Contains((int)level - 1))
            {
                _usedList.Add((int)level - 1);
                _usedList.Sort();
            }
        }
    }

    /// <summary>
    /// Adds IP numbers using a mask for range where the mask specifies the number of
    /// fixed bits, ex. 192.168.1.0/24 which will add 192.168.1.0 - 192.168.1.255
    /// </summary>
    public void Add(string ipNumber, int maskLevel)
    {
        this.Add(parseIP(ipNumber), (uint)_maskList.GetKey(_maskList.IndexOfValue(maskLevel)));
    }

    /// <summary>
    /// Adds IP numbers using a from and to IP number. The method checks the range and
    /// splits it into normal ip/mask blocks.
    /// </summary>
    public void AddRange(string fromIP, string toIP)
    {
        this.AddRange(parseIP(fromIP), parseIP(toIP));
    }

    /// <summary>
    /// Adds IP numbers using a from and to IP number. The method checks the range and
    /// splits it into normal ip/mask blocks.
    /// </summary>
    public void AddRange(uint fromIP, uint toIP)
    {
        // If the order is not asending, switch the IP numbers.
        if (fromIP > toIP)
        {
            uint tempIP = fromIP;
            fromIP = toIP;
            toIP = tempIP;
        }
        if (fromIP == toIP)
        {
            Add(fromIP);
        }
        else
        {
            uint diff = toIP - fromIP;
            int diffLevel = 1;
            uint range = 0x80000000;
            if (diff < 256)
            {
                diffLevel = 24;
                range = 0x00000100;
            }
            while (range > diff)
            {
                range = range >> 1;
                diffLevel++;
            }
            uint mask = (uint)_maskList.GetKey(_maskList.IndexOfValue(diffLevel));
            uint minIP = fromIP & mask;
            if (minIP < fromIP)
                minIP += range;
            if (minIP > fromIP)
            {
                AddRange(fromIP, minIP - 1);
                fromIP = minIP;
            }
            if (fromIP == toIP)
            {
                Add(fromIP);
            }
            else
            {
                if ((minIP + (range - 1)) <= toIP)
                {
                    Add(minIP, mask);
                    fromIP = minIP + range;
                }
                if (fromIP == toIP)
                {
                    Add(toIP);
                }
                else
                {
                    if (fromIP < toIP)
                        AddRange(fromIP, toIP);
                }
            }
        }
    }

    /// <summary>
    /// Checks if an IP number is contained in the lists, ex. 10.0.0.1
    /// </summary>
    public bool CheckNumber(string ipNumber)
    {
        return this.CheckNumber(parseIP(ipNumber));
        ;
    }

    /// <summary>
    /// Checks if an IP number is contained in the lists, ex. 0x0A000001
    /// </summary>
    public bool CheckNumber(uint ip)
    {
        bool found = false;
        int i = 0;
        while (!found && i < _usedList.Count)
        {
            found = ((IPArrayList)_ipRangeList[(int)_usedList[i]]).Check(ip);
            i++;
        }
        return found;
    }

    /// <summary>
    /// Clears all lists of IP numbers
    /// </summary>
    public void Clear()
    {
        foreach (int i in _usedList)
        {
            ((IPArrayList)_ipRangeList[i]).Clear();
        }
        _usedList.Clear();
    }

    /// <summary>
    /// Generates a list of all IP ranges in printable format
    /// </summary>
    public override string ToString()
    {
        StringBuilder buffer = new StringBuilder();
        foreach (int i in _usedList)
        {
            buffer.Append("\r\nRange with mask of ").Append(i + 1).Append("\r\n");
            buffer.Append(((IPArrayList)_ipRangeList[i]).ToString());
        }
        return buffer.ToString();
    }


}
