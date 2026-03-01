using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine;


using static Lang;

namespace Elin_Mod
{
	public class Debug_AnalyzeElin
	{
		const string c_ElemSeparator = "\t";
		const string c_ArraySeparator = ",";
		const string c_StringEncloser = "\"";

		static Texture2D _CopyTexture( Texture2D org ) {
			RenderTexture renderTexture = RenderTexture.GetTemporary(
					org.width,
					org.height,
					0,
					RenderTextureFormat.Default,
					RenderTextureReadWrite.Linear);

			Graphics.Blit(org, renderTexture);
			RenderTexture previous = RenderTexture.active;
			RenderTexture.active = renderTexture;
			Texture2D readableTextur2D = new Texture2D(org.width, org.height);
			readableTextur2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
			readableTextur2D.Apply();
			RenderTexture.active = previous;
			RenderTexture.ReleaseTemporary(renderTexture);
			return readableTextur2D;
		}


		public static void Dump_ElinSprites(string dumpPath) {
			var copyTextures = new Dictionary<string, Texture2D>();
			foreach (var itr in SpriteSheet.dict) {
				var orgTex = itr.Value.texture;
				if ( !copyTextures.ContainsKey( orgTex.name ) ) {
					copyTextures.Add(orgTex.name, _CopyTexture(orgTex));
				}
			}
			foreach ( var itr in SpriteSheet.dict ) {
				var sprite = itr.Value;
				var texRect = sprite.textureRect;
				var tex = new Texture2D((int)texRect.width, (int)texRect.height, TextureFormat.ARGB32, false );

				Texture2D copyTex = null;
				copyTextures.TryGetValue(sprite.texture.name, out copyTex);

				var spritePixels = copyTex.GetPixels((int)texRect.x, (int)texRect.y, (int)texRect.width, (int)texRect.height);
				tex.SetPixels( spritePixels );
				
				string path = $"{dumpPath}/{itr.Key}.png";
				var png = tex.EncodeToPNG();
				System.IO.File.WriteAllBytes(path, png);
				GameObject.Destroy(tex);
				tex = null;
			}
			foreach ( var itr in copyTextures) {
				GameObject.Destroy(itr.Value);
			}
			copyTextures.Clear();
			copyTextures = null;
		}


		public static void Dump_ElinZoneAll(string dumpPath) {
			var cards = EClass.sources.zones;
			var sb = _CreateSb(cards.map.Count, 1000);
			_AddHeader("id", "String");
			_AddHeader("parent", "String");
			_AddHeader("name_JP", "String");
			_AddHeader("name", "String");
			_AddHeader("type", "String");
			_AddHeader("LV", "Int");
			_AddHeader("chance", "Int");
			_AddHeader("faction", "String");
			_AddHeader("m_Values", "Int");
			_AddHeader("idProfile", "String");
			_AddHeader("idFile", "String[]");
			_AddHeader("idBiome", "String");
			_AddHeader("idGen", "String");
			_AddHeader("idPlaylist", "String");
			_AddHeader("tag", "String[]");
			_AddHeader("cost", "Int");
			_AddHeader("dev", "Int");
			_AddHeader("image", "String");
			_AddHeader("pos", "Int[]");
			_AddHeader("questTag", "String[]");
			_AddHeader("textFlavor_JP", "String");
			_AddHeader("textFlavor", "String");
			_AddHeader("detail_JP", "String");
			_AddHeader("detail", "String");
			_DumpHeader(sb);

			foreach (var itr in cards.map) {
				var a = itr.Value;
				_Dump(sb, a.id);
				_Dump(sb, a.parent);
				_Dump(sb, a.name_JP);
				_Dump(sb, a.name);
				_Dump(sb, a.type);
				_Dump(sb, a.LV);
				_Dump(sb, a.chance);
				_Dump(sb, a.faction);
				_Dump(sb, a.value);
				_Dump(sb, a.idProfile);
				_Dump(sb, a.idFile);
				_Dump(sb, a.idBiome);
				_Dump(sb, a.idGen);
				_Dump(sb, a.idPlaylist);
				_Dump(sb, a.tag);
				_Dump(sb, a.cost);
				_Dump(sb, a.dev);
				_Dump(sb, a.image);
				_Dump(sb, a.pos);
				_Dump(sb, a.questTag);
				_Dump(sb, a.textFlavor_JP);
				_Dump(sb, a.textFlavor);
				_Dump(sb, a.detail_JP);
				_Dump(sb, a.detail);

				sb.AppendLine();
			}
			_Save(dumpPath, sb);
		}

		public static void Dump_ElinCharaAll( string dumpPath ) {
			var cards = EClass.sources.charas;
			var sb = _CreateSb(cards.map.Count, 1000);
			_AddHeader("id", "String");
			_AddHeader("_id", "Int");
			_AddHeader("name_JP", "String");
			_AddHeader("name", "String");
			_AddHeader("aka_JP", "String");
			_AddHeader("aka", "String");
			_AddHeader("idActor", "String[]");
			_AddHeader("sort", "Int");
			_AddHeader("size", "Int[]");
			_AddHeader("_idRenderData", "String");
			_AddHeader("tiles", "Int[]");
			_AddHeader("tiles_snow", "Int[]");
			_AddHeader("colorMod", "Int");
			_AddHeader("components", "String[]");
			_AddHeader("defMat", "String");
			_AddHeader("LV", "Int");
			_AddHeader("chance", "Int");
			_AddHeader("quality", "Int");
			_AddHeader("hostility", "String");
			_AddHeader("biome", "String");
			_AddHeader("tag", "String[]");
			_AddHeader("trait", "String[]");
			_AddHeader("race", "String");
			_AddHeader("job", "String");
			_AddHeader("tactics", "String");
			_AddHeader("aiIdle", "String");
			_AddHeader("aiParam", "Int[]");
			_AddHeader("actCombat", "String[]");
			_AddHeader("mainElement", "String[]");
			_AddHeader("elements", "element");
			_AddHeader("equip", "String");
			_AddHeader("loot", "String[]");
			_AddHeader("category", "String");
			_AddHeader("filter", "String[]");
			_AddHeader("gachaFilter", "String[]");
			_AddHeader("tone", "String");
			_AddHeader("actIdle", "String[]");
			_AddHeader("lightData", "String");
			_AddHeader("idExtra", "String");
			_AddHeader("bio", "String");
			_AddHeader("faith", "String");
			_AddHeader("works", "String[]");
			_AddHeader("hobbies", "String[]");
			_AddHeader("idText", "String");
			_AddHeader("moveAnime", "String");
			_AddHeader("factory", "String[]");
			_AddHeader("components", "String[]");
			_AddHeader("detail_JP", "String");
			_AddHeader("detail", "String");
			_DumpHeader(sb);

			foreach (var itr in cards.map) {
				var a = itr.Value;
				_Dump(sb, a.id);
				_Dump(sb, a._id);
				_Dump(sb, a.name_JP);
				_Dump(sb, a.name);
				_Dump(sb, a.aka_JP);
				_Dump(sb, a.aka);
				_Dump(sb, a.idActor);
				_Dump(sb, a.sort);
				_Dump(sb, a.size);
				_Dump(sb, a._idRenderData);
				_Dump(sb, a.tiles);
				_Dump(sb, a.tiles_snow);
				_Dump(sb, a.colorMod);
				_Dump(sb, a.components);
				_Dump(sb, a.defMat);
				_Dump(sb, a.LV);
				_Dump(sb, a.chance);
				_Dump(sb, a.quality);
				_Dump(sb, a.hostility);
				_Dump(sb, a.biome);
				_Dump(sb, a.tag);
				_Dump(sb, a.trait);
				_Dump(sb, a.race);
				_Dump(sb, a.job);
				_Dump(sb, a.tactics);
				_Dump(sb, a.aiIdle);
				_Dump(sb, a.aiParam);
				_Dump(sb, a.actCombat);
				_Dump(sb, a.mainElement);
				_Dump(sb, a.elements);
				_Dump(sb, a.equip);
				_Dump(sb, a.loot);
				_Dump(sb, a.category);
				_Dump(sb, a.filter);
				_Dump(sb, a.gachaFilter);
				_Dump(sb, a.tone);
				_Dump(sb, a.actIdle);
				_Dump(sb, a.lightData);
				_Dump(sb, a.idExtra);
				_Dump(sb, a.bio);
				_Dump(sb, a.faith);
				_Dump(sb, a.works);
				_Dump(sb, a.hobbies);
				_Dump(sb, a.idText);
				_Dump(sb, a.moveAnime);
				_Dump(sb, a.factory);
				_Dump(sb, a.components);
				_Dump(sb, a.detail_JP);
				_Dump(sb, a.detail);

				sb.AppendLine();
			}
			_Save(dumpPath, sb);
		}


		public static void Dump_ElinThingAll( string dumpPath ) {
			var cards = EClass.sources.things;
			var sb = _CreateSb(cards.map.Count, 1000);
			_AddHeader( "id", "string" );
			_AddHeader( "name_JP" , "string" );
			_AddHeader( "unknown_JP" , "string" );
			_AddHeader( "unit_JP" , "string" );
			_AddHeader( "naming" , "string" );
			_AddHeader( "name" , "string" );
			_AddHeader( "unit" , "string" );
			_AddHeader( "unknown" , "string" );
			_AddHeader( "category" , "string" );
			_AddHeader( "sort" , "int" );
			_AddHeader( "_tileType" , "string" );
			_AddHeader( "_idRenderData" , "string" );
			_AddHeader( "tiles" , "int[]" );
			_AddHeader( "altTiles" , "int[]" );
			_AddHeader( "anime", "int[]");
			_AddHeader( "skins", "int[]");
			_AddHeader( "size", "int[]");
			_AddHeader( "colorMod", "int");
			_AddHeader( "colorType" , "string" );
			_AddHeader( "recipeKey" , "string[]" );
			_AddHeader( "factory" , "string[]" );
			_AddHeader( "components", "string[]");
			_AddHeader( "disassemble", "string[]");
			_AddHeader( "defMat" , "string" );
			_AddHeader( "tierGroup" , "string" );
			_AddHeader( "m_Values" , "int" );
			_AddHeader( "LV" , "int" );
			_AddHeader( "chance", "int");
			_AddHeader( "quality", "int");
			_AddHeader( "HP", "int");
			_AddHeader( "weight", "int");
			_AddHeader( "electricity", "int");
			_AddHeader( "trait" , "string[]" );
			_AddHeader( "elements" , "elements" );
			_AddHeader( "range", "int" );
			_AddHeader( "attackType" , "string" );
			_AddHeader( "offense" , "int[]" );
			_AddHeader( "substats" , "int[]" );
			_AddHeader( "defense" , "int[]" );
			_AddHeader( "lightData" , "string" );
			_AddHeader( "idExtra" , "string" );
			_AddHeader( "idToggleExtra", "string");
			_AddHeader( "idActorEx", "string");
			_AddHeader( "idSound", "string");
			_AddHeader( "tag", "string[]");
			_AddHeader( "workTag", "string");
			_AddHeader( "filter", "string[]");
			_AddHeader( "roomName_JP", "string[]");
			_AddHeader( "roomName", "string[]");
			_AddHeader( "detail_JP", "string");
			_AddHeader( "detail", "string");
			_DumpHeader(sb);

			foreach (var itr in cards.map) {
				var a = itr.Value;
				_Dump(sb, a.id);
				_Dump(sb, a.name_JP);
				_Dump(sb, a.unknown_JP);
				_Dump(sb, a.unit_JP);
				_Dump(sb, a.naming);
				_Dump(sb, a.name);
				_Dump(sb, a.unit);
				_Dump(sb, a.unknown);
				_Dump(sb, a.category);
				_Dump(sb, a.sort);
				_Dump(sb, a._tileType);
				_Dump(sb, a._idRenderData);
				_Dump(sb, a.tiles);
				_Dump(sb, a.altTiles);
				_Dump(sb, a.anime);
				_Dump(sb, a.skins);
				_Dump(sb, a.size);
				_Dump(sb, a.colorMod);
				_Dump(sb, a.colorType);
				_Dump(sb, a.recipeKey);
				_Dump(sb, a.factory);
				_Dump(sb, a.components);
				_Dump(sb, a.disassemble);
				_Dump(sb, a.defMat);
				_Dump(sb, a.tierGroup);
				_Dump(sb, a.value);
				_Dump(sb, a.LV);
				_Dump(sb, a.chance);
				_Dump(sb, a.quality);
				_Dump(sb, a.HP);
				_Dump(sb, a.weight);
				_Dump(sb, a.electricity);
				_Dump(sb, a.trait);
				_Dump(sb, a.elements);
				_Dump(sb, a.range);
				_Dump(sb, a.attackType);
				_Dump(sb, a.offense);
				_Dump(sb, a.substats);
				_Dump(sb, a.defense);
				_Dump(sb, a.lightData);
				_Dump(sb, a.idExtra);
				_Dump(sb, a.idToggleExtra);
				_Dump(sb, a.idActorEx);
				_Dump(sb, a.idSound);
				_Dump(sb, a.tag);
				_Dump(sb, a.workTag);
				_Dump(sb, a.filter);
				_Dump(sb, a.roomName_JP);
				_Dump(sb, a.roomName);
				_Dump(sb, a.detail_JP);
				_Dump(sb, a.detail);
				sb.AppendLine();
			}
			_Save(dumpPath, sb);
		}


		public static void Dump_ElinElementAll(string dumpPath) {
			var cards = EClass.sources.elements;
			var sb = _CreateSb(cards.map.Count, 2000);
			
			_AddHeader( "id" , "int");
			_AddHeader( "alias" , "string" );
			_AddHeader( "name_JP" , "string" );
			_AddHeader( "name" , "string" );
			_AddHeader( "altname_JP" , "string" );
			_AddHeader( "altname" , "string" );
			_AddHeader( "aliasParent" , "string" );
			_AddHeader( "aliasRef" , "string" );
			_AddHeader( "aliasMtp" , "string" );
			_AddHeader( "parentFactor" , "float" );
			_AddHeader( "lvFactor" , "int" );
			_AddHeader( "encFactor" , "int" );
			_AddHeader( "encSlot" , "string" );
			_AddHeader( "mtp" , "int" );
			_AddHeader( "LV" , "int" );
			_AddHeader( "chance" , "int" );
			_AddHeader( "m_Values" , "int" );
			_AddHeader( "cost" , "int[]" );
			_AddHeader( "geneSlot" , "int" );
			_AddHeader( "sort" , "int" );
			_AddHeader( "target" , "string" );
			_AddHeader( "proc" , "string[]" );
			_AddHeader( "type" , "string" );
			_AddHeader( "group" , "string" );
			_AddHeader( "category" , "string" );
			_AddHeader( "categorySub" , "string" );
			_AddHeader( "abilityType" , "string[]" );
			_AddHeader( "tag" , "string[]" );
			_AddHeader( "m_Thing" , "string" );
			_AddHeader( "eleP" , "int" );
			_AddHeader( "cooldown" , "int" );
			_AddHeader( "charge" , "int" );
			_AddHeader( "radius" , "float" );
			_AddHeader( "max" , "int" );
			_AddHeader( "req" , "string[]" );
			_AddHeader( "idTrainer" , "string" );
			_AddHeader( "partySkill" , "int" );
			_AddHeader( "tagTrainer" , "string" );
			_AddHeader( "levelBonus_JP" , "string" );
			_AddHeader( "levelBonus" , "string" );
			_AddHeader( "foodEffect" , "string[]" );
			_AddHeader("????", "?????");
			_AddHeader( "langAct" , "string[]" );
			_AddHeader( "detail_JP" , "string" );
			_AddHeader( "detail" , "string" );
			_AddHeader( "textPhase_JP" , "string" );
			_AddHeader( "textPhase" , "string" );
			_AddHeader( "textExtra_JP" , "string" );
			_AddHeader( "textExtra" , "string" );
			_AddHeader( "textInc_JP" , "string" );
			_AddHeader( "textInc" , "string" );
			_AddHeader( "textDec_JP" , "string" );
			_AddHeader( "textDec" , "string" );
			_AddHeader( "textAlt_JP" , "string[]" );
			_AddHeader( "textAlt" , "string[]" );
			_AddHeader( "adjective_JP" , "string[]" );
			_AddHeader( "adjective" , "string[]" );
			_DumpHeader(sb);

			foreach (var itr in cards.map) {
				var a = itr.Value;
				_Dump(sb, a.id);
				_Dump(sb, a.alias);
				_Dump(sb, a.name_JP);
				_Dump(sb, a.name);
				_Dump(sb, a.altname_JP);
				_Dump(sb, a.altname);
				_Dump(sb, a.aliasParent);
				_Dump(sb, a.aliasRef);
				_Dump(sb, a.aliasMtp);
				_Dump(sb, a.parentFactor);
				_Dump(sb, a.lvFactor);
				_Dump(sb, a.encFactor);
				_Dump(sb, a.encSlot);
				_Dump(sb, a.mtp);
				_Dump(sb, a.LV);
				_Dump(sb, a.chance);
				_Dump(sb, a.value);
				_Dump(sb, a.cost);
				_Dump(sb, a.geneSlot);
				_Dump(sb, a.sort);
				_Dump(sb, a.target);
				_Dump(sb, a.proc);
				_Dump(sb, a.type);
				_Dump(sb, a.group);
				_Dump(sb, a.category);
				_Dump(sb, a.categorySub);
				_Dump(sb, a.abilityType);
				_Dump(sb, a.tag);
				_Dump(sb, a.thing);
				_Dump(sb, a.eleP);
				_Dump(sb, a.cooldown);
				_Dump(sb, a.charge);
				_Dump(sb, a.radius);
				_Dump(sb, a.max);
				_Dump(sb, a.req);
				_Dump(sb, a.idTrainer);
				_Dump(sb, a.partySkill);
				_Dump(sb, a.tagTrainer);
				_Dump(sb, a.levelBonus_JP);
				_Dump(sb, a.levelBonus);
				_Dump(sb, a.foodEffect);
				_Dump(sb, "---");
				_Dump(sb, a.langAct);
				_Dump(sb, a.detail_JP);
				_Dump(sb, a.detail);
				_Dump(sb, a.textPhase_JP);
				_Dump(sb, a.textPhase);
				_Dump(sb, a.textExtra_JP);
				_Dump(sb, a.textExtra);
				_Dump(sb, a.textInc_JP);
				_Dump(sb, a.textInc);
				_Dump(sb, a.textDec_JP);
				_Dump(sb, a.textDec);
				_Dump(sb, a.textAlt_JP);
				_Dump(sb, a.textAlt);
				_Dump(sb, a.adjective_JP);
				_Dump(sb, a.adjective);

				sb.AppendLine();
			}

			_Save(dumpPath, sb);
		}





		public static void Dump_ElinRecipeAll(string dumpPath) {
			var cards = EClass.sources.recipes;
			var sb = _CreateSb(cards.map.Count, 500);
			_AddHeader( "id" , "string" );
			_AddHeader( "factory" , "string" );
			_AddHeader( "type" , "string" );
			_AddHeader( "m_Thing" , "string");
			_AddHeader( "m_Num" , "string" );
			_AddHeader( "sp" , "int" );
			_AddHeader( "time" , "int" );
			_AddHeader( "ing1" , "string[]" );
			_AddHeader( "ing2" , "string[]" );
			_AddHeader( "ing3" , "string[]" );
			_AddHeader( "tag" , "string[]" );
			_DumpHeader(sb);

			foreach (var itr in cards.map) {
				var a = itr.Value;
				_Dump(sb, a.id);
				_Dump(sb, a.factory);
				_Dump(sb, a.type);
				_Dump(sb, a.thing);
				_Dump(sb, a.num);
				_Dump(sb, a.sp);
				_Dump(sb, a.time);
				_Dump(sb, a.ing1);
				_Dump(sb, a.ing2);
				_Dump(sb, a.ing3);
				_Dump(sb, a.tag);
				sb.AppendLine();
			}

			_Save(dumpPath, sb);
		}

		public static void Dump_ElinFactionAll(string dumpPath) {
			var cards = EClass.sources.factions;
			var sb = _CreateSb(cards.map.Count, 1000);
			_AddHeader( "id" , "string" );
			_AddHeader("name_JP", "string" );
			_AddHeader("name", "string" );
			_AddHeader("type", "string");
			_AddHeader( "faith", "string" );
			_AddHeader( "domain" , "string" );
			_AddHeader( "relation" , "int" );
			_AddHeader( "textType_JP" , "string" );
			_AddHeader( "textType" , "string" );
			_AddHeader( "textAvatar" , "string" );
			_AddHeader( "detail_JP" , "string" );
			_AddHeader( "detail" , "string" );
			_AddHeader( "name_L" , "string" );
			_AddHeader( "detail_L" , "string" );
			_AddHeader( "textType_L" , "string" );
			_AddHeader( "textBenefit_L" , "string" );
			_AddHeader( "textPet_L" , "string" );
			_DumpHeader(sb);
			
			foreach (var itr in cards.map) {
				var a = itr.Value;
				_Dump(sb, a.id);
				_Dump(sb, a.name_JP);
				_Dump(sb, a.name);
				_Dump(sb, a.type);
				_Dump(sb, a.faith);
				_Dump(sb, a.domain);
				_Dump(sb, a.relation);
				_Dump(sb, a.textType_JP);
				_Dump(sb, a.textType);
				_Dump(sb, a.textAvatar);
				_Dump(sb, a.detail_JP);
				_Dump(sb, a.detail);
				_Dump(sb, a.name_L);
				_Dump(sb, a.detail_L);
				_Dump(sb, a.textType_L);
				_Dump(sb, a.textBenefit_L);
				_Dump(sb, a.textPet_L);
				sb.AppendLine();
			}

			_Save(dumpPath, sb);
		}



		public static void Dump_ElinCategoriesAll(string dumpPath) {
			var cards = EClass.sources.categories;
			var sb = _CreateSb(cards.map.Count, 1000);
			_AddHeader("id", "string");
			_AddHeader("uid", "int");
			_AddHeader("name_JP", "string");
			_AddHeader("name", "string");
			_AddHeader("_parent", "string");
			_AddHeader("recipeCat", "string");
			_AddHeader("slot", "element");
			_AddHeader("skill", "element");
			_AddHeader("maxStack", "int");
			_AddHeader("tileDummy", "int");
			_AddHeader("installOne", "bool");
			_AddHeader("ignoreBless", "int");
			_AddHeader("tag", "string[]");
			_AddHeader("idThing", "string");
			_AddHeader("recycle", "string[]");
			_AddHeader("costSP", "int");
			_AddHeader("gift", "int");
			_AddHeader("deliver", "int");
			_AddHeader("offer", "int");
			_AddHeader("ticket", "int");
			_AddHeader("sortVal", "int");
			_AddHeader("flag", "int");
			_DumpHeader(sb);

			foreach (var itr in cards.map) {
				var a = itr.Value;
				_Dump(sb, a.id);
				_Dump(sb, a.uid);
				_Dump(sb, a.name_JP);
				_Dump(sb, a.name);
				_Dump(sb, a._parent);
				_Dump(sb, a.recipeCat);
				_Dump(sb, a.slot);
				_Dump(sb, a.skill);
				_Dump(sb, a.maxStack);
				_Dump(sb, a.tileDummy);
				_Dump(sb, a.installOne);
				_Dump(sb, a.ignoreBless);
				_Dump(sb, a.tag);
				_Dump(sb, a.idThing);
				_Dump(sb, a.recycle);
				_Dump(sb, a.costSP);
				_Dump(sb, a.gift);
				_Dump(sb, a.deliver);
				_Dump(sb, a.offer);
				_Dump(sb, a.ticket);
				_Dump(sb, a.sortVal);
				_Dump(sb, a.flag);
				sb.AppendLine();
			}

			_Save(dumpPath, sb);
		}

		public static void Dump_ElinLangGeneral(string dumpPath) {
			var cards = EClass.sources.langGeneral;
			var sb = _CreateSb(cards.map.Count, 1000);
			_AddHeader( "id", "string" );
			_AddHeader( "filter", "string" );
			_AddHeader( "text_JP", "string" );
			_AddHeader( "text", "string" );

			_DumpHeader(sb);

			foreach (var itr in cards.map) {
				var a = itr.Value;
				_Dump(sb, a.id);
				_Dump(sb, a.filter);
				_Dump(sb, a.text_JP);
				_Dump(sb, a.text);
				sb.AppendLine();
			}

			_Save(dumpPath, sb);
		}

		public static void Dump_ElinLangList(string dumpPath) {
			var cards = EClass.sources.langList;
			var sb = _CreateSb(cards.map.Count, 1000);
			_AddHeader("id", "string" );
			_AddHeader("filter", "string" );
			_AddHeader("text_JP", "string" );
			_AddHeader("text", "string");
			_DumpHeader(sb);

			foreach (var itr in cards.map) {
				var a = itr.Value;
				_Dump(sb, a.id);
				_Dump(sb, a.filter);
				_Dump(sb, a.text_JP);
				_Dump(sb, a.text);
				sb.AppendLine();
			}

			_Save(dumpPath, sb);
		}

		public static void Dump_ElinLangGame(string dumpPath) {
			var cards = EClass.sources.langGame;
			var sb = _CreateSb(cards.map.Count, 1000);
			_AddHeader("id", "string" );
			_AddHeader("filter", "string" );
			_AddHeader("group", "string" );
			_AddHeader("color", "string" );
			_AddHeader("logColor", "string" );
			_AddHeader("sound", "string" );
			_AddHeader("effect", "string" );
			_AddHeader("text_JP", "string" );
			_AddHeader("text", "string" );
			_DumpHeader(sb);

			foreach (var itr in cards.map) {
				var a = itr.Value;
				_Dump(sb, a.id);
				_Dump(sb, a.filter);
				_Dump(sb, a.group);
				_Dump(sb, a.color);
				_Dump(sb, a.logColor);
				_Dump(sb, a.sound);
				_Dump(sb, a.effect);
				_Dump(sb, a.text_JP);
				_Dump(sb, a.text);
				sb.AppendLine();
			}

			_Save(dumpPath, sb);
		}

		public static void Dump_ElinLangWord(string dumpPath) {
			var cards = EClass.sources.langWord;
			var sb = _CreateSb(cards.map.Count, 1000);
			_AddHeader("id", "string");
			_AddHeader("group", "string");
			_AddHeader("name_JP", "string");
			_AddHeader("name", "string");
			_DumpHeader(sb);

			foreach (var itr in cards.map) {
				var a = itr.Value;
				_Dump(sb, a.id);
				_Dump(sb, a.group);
				_Dump(sb, a.name_JP);
				_Dump(sb, a.name);
				sb.AppendLine();
			}

			_Save(dumpPath, sb);
		}


		public static void Dump_ElinLangNote(string dumpPath) {
			var cards = EClass.sources.langNote;
			var sb = _CreateSb(cards.map.Count, 1000);
			_AddHeader("id", "string");
			_AddHeader("text_JP", "string");
			_AddHeader("text", "string");
			_DumpHeader(sb);

			foreach (var itr in cards.map) {
				var a = itr.Value;
				_Dump(sb, a.id);
				_Dump(sb, a.text_JP);
				_Dump(sb, a.text);
				sb.AppendLine();
			}

			_Save(dumpPath, sb);
		}



		static void _Save( string path, StringBuilder sb ) {
			if (System.IO.File.Exists(path))
				System.IO.File.Delete(path);
			System.IO.File.WriteAllText(path, sb.ToString());
		}

		static void _Dump<T>(System.Text.StringBuilder sb, T dat, bool isArrayElem=false) {
			if (dat == null) {
				sb.Append(c_ElemSeparator);
				return;
			}
			var type = dat.GetType();
			if (type.IsArray) {
				var array = dat as Array;
				bool isStrElem = type.GetElementType() == typeof(string);
				if ( isStrElem)
					sb.Append(c_StringEncloser);

				for (int i = 0; i < array.Length; ++i) {
					var d = array.GetValue(i);
					if (i > 0)
						sb.Append(c_ArraySeparator);
					_Dump(sb, d, true);
				}

				if (isStrElem)
					sb.Append(c_StringEncloser);

				sb.Append(c_ElemSeparator);
			} else {
				bool isStrElem = dat is string;
				if (!isArrayElem && isStrElem)
					sb.Append(c_StringEncloser);

				sb.Append(dat);

				if (!isArrayElem && isStrElem)
					sb.Append(c_StringEncloser);
				
				if (!isArrayElem)
					sb.Append(c_ElemSeparator);
			}
		}

		static List<(string, string)> s_Headers;

		static void _AddHeader(string label, string type) {
			if (s_Headers == null)
				s_Headers = new List<(string, string)>();
			s_Headers.Add((label, type));
		}

		static void _DumpHeader( StringBuilder sb ) {
			for ( int i = 0; i < s_Headers.Count; ++i ) 
				sb.Append(s_Headers[i].Item1).Append(c_ElemSeparator);
			sb.AppendLine();
			for (int i = 0; i < s_Headers.Count; ++i)
				sb.Append(s_Headers[i].Item2).Append(c_ElemSeparator);
			sb.AppendLine();
			sb.AppendLine();
			s_Headers.Clear();
		}

		static System.Text.StringBuilder _CreateSb(int elemCount, int dataSize) {
			return new System.Text.StringBuilder(dataSize * (elemCount + 2));
		}
	}
}
