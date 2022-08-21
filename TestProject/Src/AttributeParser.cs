using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public enum eReflectionTokenType
{
    INVALID, OPEN_BRACE, CLOSE_BRACE, IGNORED, INVERTED_COMMA, IDENT, EQUAL, STRING
};

public class ReflectionToken
{
    public eReflectionTokenType mTokenType;
    public string mName;
    public string mValue;
    public List<ReflectionToken> mChildren = new List<ReflectionToken>();
    public ReflectionToken mParent;
    public static ReflectionToken mNullToken = new ReflectionToken();

    public ReflectionToken(ReflectionToken Other)
    {
        mTokenType = Other.mTokenType;
        mName = Other.mName;
        mValue = Other.mValue;
        mParent = Other.mParent;

        foreach (ReflectionToken child in Other.mChildren)
        {
            var newChild = new ReflectionToken(child);
            newChild.mParent = this;
            mChildren.Add(newChild);
        }
    }

    public ReflectionToken(eReflectionTokenType TokenType = eReflectionTokenType.INVALID, string Name = "", ReflectionToken Parent = null, string Value = "")
    {
        mTokenType = TokenType;
        mName = Name;
        mParent = Parent;
        mValue = Value;
    }

    public void Insert(ReflectionToken Other)
    {
        mChildren.AddRange(Other.mChildren);
    }

    public void SetChild(string Name, string Value)
    {
        ReflectionToken child = GetChild(Name);
        if (child == null)
        {
            child = AddChild(Name);
        }

        child.mValue = Value;
    }

    public void AddChild(string Name, string Value)
    {
        mChildren.Add(new ReflectionToken(eReflectionTokenType.IDENT, Name, this, Value));
    }

    public ReflectionToken AddChild(string Name)
    {
        mChildren.Add(new ReflectionToken(eReflectionTokenType.IDENT, Name, this));
        return mChildren.Last();
    }

    public void AddChild(ReflectionToken Child)
    {
        mChildren.Add(Child);
    }

    public ReflectionToken GetChild(string Name)
    {
        foreach (ReflectionToken child in mChildren)
        {
            if (child.mName == Name)
            {
                return child;
            }
        }

        return null;
    }

    public ReflectionToken GetChildSafe(string Name)
    {
        foreach (ReflectionToken child in mChildren)
        {
            if (child.mName == Name)
            {
                return child;
            }
        }

        return mNullToken;
    }

    public ReflectionToken GetNextChild(ReflectionToken Previous)
    {
        int i = 0;
        for (; i < mChildren.Count; i++)
        {
            if (mChildren[i] == Previous)
            {
                i++;
                break;
            }
        }

        for (; i < mChildren.Count; i++)
        {
            if (mChildren[i].mName == Previous.mName)
            {
                return mChildren[i];
            }
        }

        return null;
    }


    public string GetAsTypeString()
    {
        string typeStr = "";

        if (GetChildSafe("IsConst").mValue == "true")
        {
            typeStr += "const ";
        }

        if (GetChildSafe("IsDynamicArray").mValue == "true")
        {
            typeStr += "cArray<";
        }

        if (GetChildSafe("IsTInstancePtr").mValue == "true")
        {
            typeStr += "tInstancePtr<";
        }

        typeStr += GetChildSafe("Type").mValue;

        if (GetChildSafe("IsPointer").mValue == "true")
        {
            typeStr += "";
        }

        if (GetChildSafe("IsReference").mValue == "true")
        {
            typeStr += "&";
        }

        if (GetChildSafe("IsTInstancePtr").mValue == "true")
        {
            typeStr += ">";
        }

        if (GetChildSafe("IsDynamicArray").mValue == "true")
        {
            typeStr += ">";
        }

        string arraySize = GetChildSafe("ArraySize").mValue;
        if (arraySize.Length > 0)
        {
            typeStr += "[";
            typeStr += arraySize;
            typeStr += "]";
        }

        return typeStr;
    }
}

public class AttributeParser
{
    private	static ReflectionToken[] mTokenTable = {
        new ReflectionToken(eReflectionTokenType.OPEN_BRACE,       "{"),
        new ReflectionToken(eReflectionTokenType.CLOSE_BRACE,      "}"),
        new ReflectionToken(eReflectionTokenType.IGNORED,          " "),
        new ReflectionToken(eReflectionTokenType.IGNORED,          "\r"),
        new ReflectionToken(eReflectionTokenType.IGNORED,          "\n"),
        new ReflectionToken(eReflectionTokenType.IGNORED,          "\t"),
        new ReflectionToken(eReflectionTokenType.IGNORED,          ","),
        new ReflectionToken(eReflectionTokenType.IGNORED,          "\""),
        new ReflectionToken(eReflectionTokenType.INVERTED_COMMA,   "'"),
        new ReflectionToken(eReflectionTokenType.EQUAL,            "="),
        new ReflectionToken(eReflectionTokenType.INVALID,          "")
    };
    private ReflectionToken mSubRootToken;

    public AttributeParser(ReflectionToken RootToken)
    {
        mSubRootToken = RootToken;
    }

    private ReflectionToken GetRawToken(int Index, string SourceString, int SourceLength)
    {
	    int tokenIndex = 0;
	    while (mTokenTable[tokenIndex].mTokenType != eReflectionTokenType.INVALID)
	    {
		    ReflectionToken entry = mTokenTable[tokenIndex];
            int numTokenCharacters = entry.mName.Length;
            int numCharacters = Math.Min(numTokenCharacters, SourceLength - Index - numTokenCharacters);

		    if (numTokenCharacters <= SourceLength - Index && SourceString.Substring(Index, numTokenCharacters) == entry.mName)
		    {
			    return entry;
		    }

            tokenIndex++;
	    }

	    return new ReflectionToken(eReflectionTokenType.INVALID, "" );
    }

    private ReflectionToken GetToken(ref int Index, string SourceString, int SourceLength)
    {
        ReflectionToken identToken = new ReflectionToken(eReflectionTokenType.IDENT);
        ReflectionToken nextToken = new ReflectionToken(eReflectionTokenType.INVALID);

        while (Index < SourceLength && SourceString[Index] != 0)
        {
            nextToken = GetRawToken(Index, SourceString, SourceLength);
            if (nextToken.mTokenType == eReflectionTokenType.INVALID)
            {
                identToken.mName += SourceString[Index++];
            }
            else if (identToken.mName.Length != 0)
            {
                return identToken;
            }
            else if (nextToken.mTokenType == eReflectionTokenType.INVERTED_COMMA)
            {
                return GetString(ref Index, SourceString, SourceLength);
            }
            else if (nextToken.mTokenType == eReflectionTokenType.IGNORED)
            {
                Index += (int)nextToken.mName.Length;
                nextToken = new ReflectionToken(eReflectionTokenType.INVALID);
                continue;
            }
            else
            {
                Index += (int)nextToken.mName.Length;
                break;
            }
        }

        return nextToken;
    }

    private ReflectionToken GetString(ref int Index, string SourceString, int SourceLength)
    {
        ReflectionToken stringToken = new ReflectionToken(eReflectionTokenType.STRING, "");

        if ((Index < SourceLength && SourceString[Index] != '\'') || Index >= SourceLength)
        {
            return new ReflectionToken(eReflectionTokenType.INVALID, "");
        }
        Index++;

        while (Index < SourceLength && SourceString[Index] != 0 && SourceString[Index] != '\'')
        {
            stringToken.mName += SourceString[Index++];
        }

        if ((Index < SourceLength && SourceString[Index] != '\'') || Index >= SourceLength)
        {
            return new ReflectionToken(eReflectionTokenType.INVALID, "");
        }
        Index++;

        return stringToken;
    }


    private void PushToken(ReflectionToken Token)
    {
        Token.mParent = mSubRootToken;
        mSubRootToken.mChildren.Add(Token);
    }

    private void GotoChildToken()
    {
        mSubRootToken = mSubRootToken.mChildren.Last();
    }

    private void GotoParentToken()
    {
        mSubRootToken = mSubRootToken.mParent;
    }

    public bool Tokenize(string SourceString)
    {
        int index = 0;
        int sourceLength = SourceString.Length;

        ReflectionToken nextToken = GetToken(ref index, SourceString, sourceLength);
        while (nextToken.mTokenType != eReflectionTokenType.INVALID)
        {
            switch (nextToken.mTokenType)
            {
                case eReflectionTokenType.IDENT:
                    {
                        ReflectionToken identToken = nextToken;
                        nextToken = GetToken(ref index, SourceString, sourceLength);
                        if (nextToken.mTokenType != eReflectionTokenType.EQUAL)
                        {
                    //         std.cout << "Error parsing attributes for: \n" << SourceString << "\n";
                            return false;
                        }

                        nextToken = GetToken(ref index, SourceString, sourceLength);

                        if (nextToken.mTokenType == eReflectionTokenType.OPEN_BRACE)
                        {
                            PushToken(identToken);
                            continue;
                        }

                        if (nextToken.mTokenType != eReflectionTokenType.STRING)
                        {
                    //         std.cout << "Error parsing attributes for: \n" << SourceString << "\n";
                            return false;
                        }

                        identToken.mValue = nextToken.mName;
                        PushToken(identToken);
                        break;
                    }
                case eReflectionTokenType.OPEN_BRACE:
                    {
                        GotoChildToken();
                        break;
                    }
                case eReflectionTokenType.CLOSE_BRACE:
                    {
                        GotoParentToken();
                        break;
                    }
                default:
            //         std.cout << "Error parsing attributes for: \n" << SourceString << "\n";
                    return false;
            }

            nextToken = GetToken(ref index, SourceString, sourceLength);
        }

        return true;
    }
}

