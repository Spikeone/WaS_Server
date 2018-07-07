/*
Navicat MySQL Data Transfer

Source Server         : Local-DB
Source Server Version : 50621
Source Host           : localhost:3306
Source Database       : was

Target Server Type    : MYSQL
Target Server Version : 50621
File Encoding         : 65001

Date: 2018-07-07 21:34:45
*/

SET FOREIGN_KEY_CHECKS=0;

-- ----------------------------
-- Table structure for `accounts`
-- ----------------------------
DROP TABLE IF EXISTS `accounts`;
CREATE TABLE `accounts` (
  `id` int(8) NOT NULL AUTO_INCREMENT,
  `username` varchar(32) NOT NULL,
  `sha_pass` varchar(64) NOT NULL DEFAULT 'F3341DBCDDA605B1601524B0D01655750CC60E0D',
  `gm_level` int(4) NOT NULL DEFAULT '0',
  `email` varchar(64) DEFAULT NULL,
  `joindate` date DEFAULT NULL,
  `lastip` varchar(32) DEFAULT NULL,
  `lastlogin` date DEFAULT NULL,
  `status` int(4) NOT NULL DEFAULT '0',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=latin1;

-- ----------------------------
-- Records of accounts
-- ----------------------------
INSERT INTO `accounts` VALUES ('1', 'Spikeone', 'F3341DBCDDA605B1601524B0D01655750CC60E0D', '0', null, null, null, null, '0');
INSERT INTO `accounts` VALUES ('2', 'Kurothas', 'F3341DBCDDA605B1601524B0D01655750CC60E0D', '0', null, null, null, null, '0');
INSERT INTO `accounts` VALUES ('3', 'Icemax', 'F3341DBCDDA605B1601524B0D01655750CC60E0D', '0', null, null, null, null, '0');

-- ----------------------------
-- Table structure for `characters`
-- ----------------------------
DROP TABLE IF EXISTS `characters`;
CREATE TABLE `characters` (
  `guid` int(32) NOT NULL,
  `account` int(8) NOT NULL,
  `nickname` varchar(20) NOT NULL,
  `race` int(4) NOT NULL DEFAULT '0',
  `gender` int(1) NOT NULL DEFAULT '1',
  `level` int(8) NOT NULL DEFAULT '1',
  `xp` int(8) NOT NULL DEFAULT '0',
  `money` int(8) NOT NULL DEFAULT '0',
  `flags1` int(8) NOT NULL DEFAULT '0',
  `flags2` int(8) NOT NULL DEFAULT '0',
  `pos_x` float NOT NULL DEFAULT '0',
  `pos_y` float NOT NULL DEFAULT '0',
  `pos_z` float NOT NULL DEFAULT '0',
  `rot_x` float NOT NULL DEFAULT '0',
  `rot_y` float NOT NULL DEFAULT '0',
  `rot_z` float NOT NULL DEFAULT '0',
  `map` int(8) NOT NULL DEFAULT '1',
  PRIMARY KEY (`guid`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- ----------------------------
-- Records of characters
-- ----------------------------
INSERT INTO `characters` VALUES ('1', '1', 'SpikeChar', '0', '1', '1', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '1');
INSERT INTO `characters` VALUES ('2', '2', 'KurothasChar', '0', '1', '1', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '1');
INSERT INTO `characters` VALUES ('3', '3', 'IcemaxChar', '0', '1', '1', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '1');

-- ----------------------------
-- Table structure for `gameobject`
-- ----------------------------
DROP TABLE IF EXISTS `gameobject`;
CREATE TABLE `gameobject` (
  `guid` int(8) NOT NULL,
  `entry` int(8) NOT NULL,
  `map` int(8) NOT NULL DEFAULT '0',
  `pos_x` float NOT NULL DEFAULT '0',
  `pos_y` float NOT NULL DEFAULT '0',
  `pos_z` float NOT NULL DEFAULT '0',
  `rot_x` float NOT NULL DEFAULT '0',
  `rot_y` float NOT NULL DEFAULT '0',
  `rot_z` float NOT NULL DEFAULT '0',
  `spawntime` int(8) NOT NULL DEFAULT '300',
  `state` int(8) unsigned zerofill NOT NULL DEFAULT '00000000',
  PRIMARY KEY (`guid`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- ----------------------------
-- Records of gameobject
-- ----------------------------
INSERT INTO `gameobject` VALUES ('1', '1', '0', '-1.749', '0.589', '-0.204', '0', '-25.044', '0', '300', '00000000');
INSERT INTO `gameobject` VALUES ('2', '3', '0', '-20.886', '-3.388', '0.20583', '-0.008', '82.987', '-15.541', '300', '00000000');

-- ----------------------------
-- Table structure for `gameobject_template`
-- ----------------------------
DROP TABLE IF EXISTS `gameobject_template`;
CREATE TABLE `gameobject_template` (
  `entry` int(8) unsigned zerofill NOT NULL,
  `type` int(8) NOT NULL DEFAULT '0',
  `displayID` int(4) NOT NULL DEFAULT '1',
  `name` varchar(64) NOT NULL,
  `scale` float NOT NULL DEFAULT '1',
  `data0` int(8) unsigned zerofill DEFAULT '00000000',
  `data1` int(8) unsigned zerofill DEFAULT '00000000',
  `data2` int(8) unsigned zerofill DEFAULT '00000000',
  `data3` int(8) unsigned zerofill DEFAULT '00000000',
  `data4` int(8) unsigned zerofill DEFAULT '00000000',
  `data5` int(8) unsigned zerofill DEFAULT '00000000',
  `data6` int(8) unsigned zerofill DEFAULT '00000000',
  `data7` int(8) unsigned zerofill DEFAULT '00000000',
  `scriptName` varchar(64) DEFAULT NULL,
  `comment` varchar(128) DEFAULT NULL,
  PRIMARY KEY (`entry`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- ----------------------------
-- Records of gameobject_template
-- ----------------------------
INSERT INTO `gameobject_template` VALUES ('00000001', '0', '5', 'Serverside Object', '1', '00000000', '00000000', '00000000', '00000000', '00000000', '00000000', '00000000', '00000000', null, 'Object 1');
INSERT INTO `gameobject_template` VALUES ('00000002', '0', '5', 'Test2', '0', '00000000', '00000000', '00000000', '00000000', '00000000', '00000000', '00000000', '00000000', null, 'Obejct 2');
INSERT INTO `gameobject_template` VALUES ('00000003', '1', '6', 'Teleport to Map', '1', '00000001', '00000000', '00000000', '00000000', '00000000', '00000000', '00000000', '00000000', null, 'Teleport player to point 1');

-- ----------------------------
-- Table structure for `item`
-- ----------------------------
DROP TABLE IF EXISTS `item`;
CREATE TABLE `item` (
  `guid` int(8) NOT NULL AUTO_INCREMENT,
  `entry` int(8) NOT NULL DEFAULT '1',
  `owner_guid` int(8) NOT NULL,
  `state` int(8) NOT NULL DEFAULT '0',
  `loot_date` datetime DEFAULT NULL,
  `loot_from` int(8) DEFAULT NULL,
  PRIMARY KEY (`guid`)
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=latin1;

-- ----------------------------
-- Records of item
-- ----------------------------
INSERT INTO `item` VALUES ('1', '2', '1', '0', null, null);
INSERT INTO `item` VALUES ('2', '2', '1', '1', null, null);
INSERT INTO `item` VALUES ('3', '2', '1', '1', null, null);
INSERT INTO `item` VALUES ('4', '2', '1', '1', null, null);
INSERT INTO `item` VALUES ('5', '3', '1', '2', null, null);

-- ----------------------------
-- Table structure for `item_template`
-- ----------------------------
DROP TABLE IF EXISTS `item_template`;
CREATE TABLE `item_template` (
  `entry` int(8) NOT NULL,
  `class` int(8) NOT NULL DEFAULT '0',
  `subclass` int(8) NOT NULL DEFAULT '0',
  `name` varchar(64) NOT NULL DEFAULT '0',
  `display_id_main` int(8) NOT NULL DEFAULT '0',
  `display_id_off` int(8) NOT NULL DEFAULT '0',
  `quality` int(8) NOT NULL DEFAULT '0',
  `price` int(8) NOT NULL DEFAULT '0',
  `itemlevel` int(8) NOT NULL DEFAULT '0',
  `max_uses` int(8) NOT NULL DEFAULT '0',
  `allowed_class` int(8) NOT NULL DEFAULT '0',
  `allowed_race` int(8) NOT NULL DEFAULT '0',
  `req_level` int(8) NOT NULL DEFAULT '0',
  `req_skill` int(8) NOT NULL DEFAULT '0',
  `req_reputation` int(8) NOT NULL DEFAULT '0',
  `armor_magic` int(8) NOT NULL DEFAULT '0',
  `armor_physical` int(8) NOT NULL DEFAULT '0',
  `dmg_min` int(8) NOT NULL DEFAULT '0',
  `dmg_max` int(8) NOT NULL DEFAULT '0',
  `stat_type1` int(8) NOT NULL DEFAULT '0',
  `stat_value1` int(8) NOT NULL DEFAULT '0',
  `stat_type2` int(8) NOT NULL DEFAULT '0',
  `stat_value2` int(8) NOT NULL DEFAULT '0',
  `stat_type3` int(8) NOT NULL DEFAULT '0',
  `stat_value3` int(8) NOT NULL DEFAULT '0',
  `stat_type4` int(8) NOT NULL DEFAULT '0',
  `stat_value4` int(8) NOT NULL DEFAULT '0',
  PRIMARY KEY (`entry`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- ----------------------------
-- Records of item_template
-- ----------------------------
INSERT INTO `item_template` VALUES ('1', '0', '0', 'ItemTemplate1', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0');
INSERT INTO `item_template` VALUES ('2', '2', '6', 'Stick of Lies', '3', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0');
INSERT INTO `item_template` VALUES ('3', '6', '0', 'Simple Linen', '4', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0');

-- ----------------------------
-- Table structure for `pointsofinterest`
-- ----------------------------
DROP TABLE IF EXISTS `pointsofinterest`;
CREATE TABLE `pointsofinterest` (
  `entry` int(8) NOT NULL,
  `group` int(8) NOT NULL,
  `type` int(8) NOT NULL,
  `map` int(8) NOT NULL,
  `pos_x` float NOT NULL,
  `pos_y` float NOT NULL,
  `pos_z` float NOT NULL,
  `rot_x` float NOT NULL,
  `rot_y` float NOT NULL,
  `rot_z` float NOT NULL,
  `comment` varchar(128) DEFAULT NULL,
  PRIMARY KEY (`entry`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- ----------------------------
-- Records of pointsofinterest
-- ----------------------------
INSERT INTO `pointsofinterest` VALUES ('1', '0', '0', '1', '10', '1', '10', '10', '10', '10', 'Mine Entrance');
