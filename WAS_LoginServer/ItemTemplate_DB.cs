using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace WAS_LoginServer
{
    public class ItemTemplate_DB
    {
        ulong ulEntry;

        // class
        // subclass
        ulong[] ulClass = new ulong[2];
        string strName;
        // display ID mainhand
        // display ID offhand
        ulong[] ulDisplayIds = new ulong[2];
        ulong ulQuality;
        ulong ulPrice;
        ulong ulItemLevel;
        ulong ulMaxUses;

        // Allowed Classes
        // Allowed Races
        private ulong[] ulAllowances = new ulong[2];

        // Required Level
        // Required Skill
        // Required Reputation
        private ulong[] ulRequirements = new ulong[3];

        // Magic Armor
        // Physical Armor
        private ulong[] ulArmor = new ulong[2];

        // Damage min
        // Damage max
        private ulong[] ulDamage = new ulong[2];

        // Stat Type 1
        // Stat Type 2
        // Stat Type 3
        // Stat Type 4
        private ulong[] ulStatTypes = new ulong[4];

        // Stat Value 1
        // Stat Value 2
        // Stat Value 3
        // Stat Value 4
        private ulong[] ulStatValues = new ulong[4];

        public ulong getEntry() { return ulEntry; }

        public string getItemTemplateName() { return strName; }

        public ulong getItemTemplateQuality() { return ulQuality; }

        public ulong getItemTemplatePrice() { return ulPrice; }

        public ulong getItemTemplateLevel() { return ulItemLevel; }

        public ulong getItemTemplateMaxUses() { return ulMaxUses; }

        // Class

        public ulong getItemTemplateClass(ulong uiIndex)
        {
            if (uiIndex > 1)
                return 0;
            else
                return ulClass[uiIndex];
        }

        public void getItemTemplateClasses(ref ulong ulItemClass, ref ulong ulItemSubClass)
        {
            ulItemClass = ulClass[0];
            ulItemSubClass = ulClass[1];
        }

        // Display

        public ulong getItemTemplateDisplayID(ulong uiIndex)
        {
            if (uiIndex > 1)
                return 0;
            else
                return ulDisplayIds[uiIndex];
        }

        public void getItemTemplateulDisplayIds(ref ulong ulItemDisplayMain, ref ulong ulItemDisplayOff)
        {
            ulItemDisplayMain = ulDisplayIds[0];
            ulItemDisplayOff = ulDisplayIds[1];
        }

        // Allowance

        public ulong getItemTemplateAllowance(ulong uiIndex)
        {
            if (uiIndex > 1)
                return 0;
            else
                return ulAllowances[uiIndex];
        }

        public void getItemTemplateAllowance(ref ulong ulClass, ref ulong ulRace)
        {
            ulClass = ulAllowances[0];
            ulRace = ulAllowances[1];
        }

        private bool isItemTemplateAllowed(ulong ulIndex, ulong ulValue)
        {
            if (ulAllowances[ulIndex] == 0)
                return true;

            return (ulAllowances[ulIndex] & ulValue) == ulValue;
        }

        public bool isItemTemplateAllowedForClass(ulong ulClass)
        {
            return isItemTemplateAllowed(0, ulClass);
        }

        public bool isItemTemplateAllowedForRace(ulong ulRace)
        {
            return isItemTemplateAllowed(1, ulRace);
        }

        public bool isItemTemplateAllowedForCharacter(ulong ulClass, ulong ulRace)
        {
            return isItemTemplateAllowedForClass(ulClass) && isItemTemplateAllowedForRace(ulRace);
        }

        // Requirement

        public ulong getItemTemplateRequirement(ulong ulIndex)
        {
            if (ulIndex > 2)
                return 0;
            else
                return ulRequirements[ulIndex];
        }

        public void getItemTemplateRequirements(ref ulong ulLevel, ref ulong ulSkill, ref ulong ulReputation)
        {
            ulLevel = ulRequirements[0];
            ulSkill = ulRequirements[1];
            ulReputation = ulRequirements[2];
        }

        private bool meetsItemTemplateRequirement(ulong ulIndex, ulong ulValue)
        {
            if (ulRequirements[ulIndex] == 0)
                return true;

            return (ulRequirements[ulIndex] & ulValue) == ulValue;
        }

        public bool meetsItemTemplateLevelRequirement(ulong ulLevel)
        {
            return meetsItemTemplateRequirement(0, ulLevel);
        }

        public bool meetsItemTemplateSkillRequirement(ulong ulSkill)
        {
            return meetsItemTemplateRequirement(1, ulSkill);
        }

        public bool meetsItemTemplateReputationRequirement(ulong ulReputation)
        {
            return meetsItemTemplateRequirement(2, ulReputation);
        }

        public bool meetsItemTemplateRequirementForCharacter(ulong ulLevel, ulong ulSkill, ulong ulReputation)
        {
            return meetsItemTemplateLevelRequirement(ulLevel) && meetsItemTemplateSkillRequirement(ulSkill) && meetsItemTemplateReputationRequirement(ulReputation);
        }

        // Armor

        private ulong getItemTemplateArmor(ulong ulIndex)
        {
            if (ulIndex > 1)
                return 0;
            else
                return ulArmor[ulIndex];
        }

        public ulong getItemTemplateMagicArmor() { return getItemTemplateArmor(0); }

        public ulong getItemTemplatePhysicalArmor() { return getItemTemplateArmor(1); }

        public void getItemTemplateArmor(ref ulong ulMagicArmor, ref ulong ulPhysicalArmor)
        {
            ulMagicArmor = getItemTemplateArmor(0);
            ulPhysicalArmor = getItemTemplateArmor(1);
        }

        // Damage

        private ulong getItemTemplateDamage(ulong ulIndex)
        {
            if (ulIndex > 1)
                return 0;
            else
                return ulDamage[ulIndex];
        }

        public ulong getItemTemplateMinDamage() { return getItemTemplateDamage(0); }

        public ulong getItemTemplateMaxDamage() { return getItemTemplateDamage(1); }

        public void getItemTemplateDamage(ref ulong ulMinDamage, ref ulong ulMaxDamage)
        {
            ulMinDamage = getItemTemplateDamage(0);
            ulMaxDamage = getItemTemplateDamage(1);
        }

        // Stats

        private ulong getItemTemplateStatType(ulong ulIndex)
        {
            if (ulIndex > 3)
                return 0;
            else
                return ulStatTypes[ulIndex];
        }

        private ulong getItemTemplateStatValue(ulong ulIndex)
        {
            if (ulIndex > 3)
                return 0;
            else
                return ulStatValues[ulIndex];
        }

        public bool hasItemTemplateStat(ulong ulstat)
        {
            for (ulong i = 0; i < 4; i++)
                if (getItemTemplateStatType(i) == ulstat)
                    return true;

            return false;
        }

        public ulong getItemTemplateStatCount()
        {
            for (ulong i = 0; i < 4; i++)
                if (getItemTemplateStatType(i) == 0)
                    return (i + 1);

            return 0;
        }

        public void getItemTemplateStat(ulong ulIndex, ref ulong ulType, ref ulong ulValue)
        {
            if ((ulIndex + 1) > getItemTemplateStatCount())
            {
                ulType = 0;
                ulValue = 0;

            }

            ulType = getItemTemplateStatType(ulIndex);
            ulValue = getItemTemplateStatValue(ulIndex);
        }

        // Constructor

        public ItemTemplate_DB(ulong ulEntry, ulong ulClass, ulong ulSubClass, string strName, ulong ulDisplayIdMain, ulong DisplayIdOff, ulong ulQuality, ulong ulPrice, ulong ulItemLevel, ulong ulMaxUses,
                                ulong[] ulAllowances, ulong[] ulRequirements, ulong[] ulArmor, ulong[] ulDamage, ulong[] ulStatTypes, ulong[] ulStatValues)
        {
            this.ulEntry = ulEntry;
            this.ulClass[0] = ulClass;
            this.ulClass[1] = ulSubClass;
            this.strName = strName;
            this.ulDisplayIds[0] = ulDisplayIdMain;
            this.ulDisplayIds[1] = DisplayIdOff;
            this.ulQuality = ulQuality;
            this.ulPrice = ulPrice;
            this.ulItemLevel = ulItemLevel;
            this.ulMaxUses = ulMaxUses;

            for (ulong i = 0; i < 2; i++)
                this.ulAllowances[i] = ulAllowances[i];

            for (ulong i = 0; i < 3; i++)
                this.ulRequirements[i] = ulRequirements[i];

            for (ulong i = 0; i < 2; i++)
                this.ulArmor[i] = ulArmor[i];

            for (ulong i = 0; i < 2; i++)
                this.ulDamage[i] = ulDamage[i];

            for (ulong i = 0; i < 4; i++)
                this.ulStatTypes[i] = ulStatTypes[i];

            for (ulong i = 0; i < 4; i++)
                this.ulStatValues[i] = ulStatValues[i];
        }

        // Serializer

        // returns a string as following:
        // 
        public string getSerializedData(CultureInfo objFormatProvider)
        {
            string[] strData = new string[27];

            strData[0] = ulEntry.ToString(objFormatProvider);
            strData[1] = ulClass[0].ToString(objFormatProvider);
            strData[2] = ulClass[1].ToString(objFormatProvider);
            strData[3] = strName;
            strData[4] = ulDisplayIds[0].ToString(objFormatProvider);
            strData[5] = ulDisplayIds[1].ToString(objFormatProvider);
            strData[6] = ulQuality.ToString(objFormatProvider);
            strData[7] = ulPrice.ToString(objFormatProvider);
            strData[8] = ulItemLevel.ToString(objFormatProvider);
            strData[9] = ulMaxUses.ToString(objFormatProvider);

            strData[10] = ulAllowances[0].ToString(objFormatProvider);
            strData[11] = ulAllowances[1].ToString(objFormatProvider);

            strData[12] = ulRequirements[0].ToString(objFormatProvider);
            strData[13] = ulRequirements[1].ToString(objFormatProvider);
            strData[14] = ulRequirements[2].ToString(objFormatProvider);

            strData[15] = ulArmor[0].ToString(objFormatProvider);
            strData[16] = ulArmor[1].ToString(objFormatProvider);

            strData[17] = ulDamage[0].ToString(objFormatProvider);
            strData[18] = ulDamage[1].ToString(objFormatProvider);

            strData[19] = ulStatTypes[0].ToString(objFormatProvider);
            strData[20] = ulStatTypes[1].ToString(objFormatProvider);
            strData[21] = ulStatTypes[2].ToString(objFormatProvider);
            strData[22] = ulStatTypes[3].ToString(objFormatProvider);

            strData[23] = ulStatValues[0].ToString(objFormatProvider);
            strData[24] = ulStatValues[1].ToString(objFormatProvider);
            strData[25] = ulStatValues[2].ToString(objFormatProvider);
            strData[26] = ulStatValues[3].ToString(objFormatProvider);

            string strReturnString = "";

            for (ulong i = 0; i < 27; i++)
            {
                if (strReturnString != "")
                    strReturnString += "/";

                strReturnString += strData[i];
            }

            return strReturnString;
        }
    }
}
