using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Utilities;

using UnnamedContentMod.Utilities;

namespace UnnamedContentMod.Content.NPCs;

public class Monkey : ModNPC {
    private int _idleTimer = Main.rand.Next(60 * 2, 60 * 8);
    private int _idleMax = Main.rand.Next(60 * 2, 60 * 8);
    private int _walkingTimer = Main.rand.Next(60 * 2, 60 * 8);
    private int _walkingDir = Main.rand.Next(60 * 2, 60 * 8);
    private int _walkingMax = Main.rand.Next(60 * 2, 60 * 8);
    private int _jumpCooldown = Main.rand.Next(60 * 2, 60 * 8);
    private int _jumpCooldownMax = 120;
    private int _jumpStateTimer = Main.rand.Next(60 * 2, 60 * 8);
    private int _jumpStateMax = Main.rand.Next(60 * 2, 60 * 8);
    private int _climbingTimer = Main.rand.Next(60 * 5, 60 * 10);
    private int _climbingMax = Main.rand.Next(60 * 5, 60 * 10);
    private int _offeringTimer = Main.rand.Next(60 * 5, 60 * 10);
    private int _offeringMax = Main.rand.Next(60 * 5, 60 * 10); 
    
    private int _playerWhoAmI;
    private float _jumpClimbVel;
    private bool _idleOnFall;
    private State _currentState;

    private readonly int animation_speed = 5;

    public override string Texture { get; } = Images.NPCS.Jungle.KEY_Monkey;

    private enum State {
        Idle, 
        Walking,
        Jumping, 
        Climbing, 
        Offering
    }

    public override void SetStaticDefaults() {
        Main.npcFrameCount[Type] = 27;
        NPCID.Sets.TownCritter[Type] = true;
        NPCID.Sets.CountsAsCritter[Type] = true;
    }
    
    public override void SetDefaults() {
        (NPC.width, NPC.height) = (22, 22);
        
        NPC.lifeMax = 10;
        NPC.aiStyle = -1;
        NPC.HitSound = SoundID.NPCHit1;
        NPC.noGravity = true;
    }
    
    public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) {
        bestiaryEntry.AddTags(
            BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Jungle,
            new FlavorTextBestiaryInfoElement("This species of primate seems to be interested in popping balloons."));
    }
    
    public override float SpawnChance(NPCSpawnInfo spawnInfo) {
        if (Main.dayTime && spawnInfo.Player.ZoneForest) {
            return SpawnCondition.SurfaceJungle.Chance * 1f;
        }
        return 0f;
    }
    
    public override void FindFrame(int frameHeight) {
        void AnimateFrames(int firstFrame, int lastFrame, int speed) {
            int currentAbsoluteFrame = NPC.frame.Y / frameHeight;
            if (NPC.frameCounter++ >= speed) {
                NPC.frameCounter = 0;
                currentAbsoluteFrame++;
            }
            if (currentAbsoluteFrame < firstFrame || currentAbsoluteFrame > lastFrame) {
                currentAbsoluteFrame = firstFrame;
            }
            NPC.frame.Y = currentAbsoluteFrame * frameHeight;
        }
        
        if (_currentState == State.Idle || (_currentState == State.Jumping && NPC.velocity.Y == 0)) {
            AnimateFrames(0, 5, animation_speed);
        }
        else if (_currentState == State.Walking) {
            AnimateFrames(6, 11, animation_speed);
        }
        else if (_currentState == State.Jumping) {
            if (NPC.velocity.Y > 0) {
                NPC.frame.Y = 13 * frameHeight;
            }
            else if (NPC.velocity.Y < 0) {
                NPC.frame.Y = 12 * frameHeight;
            }
        }
        else if (_currentState == State.Climbing) {
            if (NPC.velocity.Y == 0) {
                NPC.frame.Y = 14 * frameHeight;
            }
            else {
                AnimateFrames(15, 20, animation_speed);
            }
        }
        else if (_currentState == State.Offering) {
            AnimateFrames(21, 26, animation_speed);
        }
    }
    
    public override void OnKill() {
        Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>($"{Name}GoreHead").Type);
        Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>($"{Name}GoreTail").Type);
        Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>($"{Name}GoreInsides").Type);
    }
    
    public override void AI() {
        if (TreeCheck() && Main.rand.NextBool(80) && _currentState == State.Walking) {
            _currentState = State.Climbing;
            if (NPC.velocity.X < 0)
            {
                NPC.position.X -= 10;
            }
        }
        if (_currentState != State.Climbing) {
            NPC.velocity.Y = MathHelper.Lerp(NPC.velocity.Y, 5, 0.1f);
        }
        switch (_currentState) {
            case State.Idle:
                Idle();
                break;
            case State.Walking:
                Walking();
                break;
            case State.Jumping:
                Jumping(_idleOnFall);
                break;
            case State.Climbing:
                Climbing();
                break;
            case State.Offering:
                Offering();
                break;
        }
    }
    public override void PostAI() {
        Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed, ref NPC.gfxOffY);
    }
    
    public void Idle() {
        _idleTimer++;
        if (_idleTimer >= _idleMax) {
            _idleTimer = 0;
            _idleMax = Main.rand.Next(60 * 2, 60 * 8);
            if (!Main.rand.NextBool(4)) {
                _currentState = State.Walking;
                _walkingDir = Main.rand.NextSign();
                NPC.spriteDirection = -_walkingDir;
            }
            else {
                _currentState = State.Jumping;
                NPC.velocity.Y = -8;
            }
        }

        for (int i = 0; i < Main.player.Length; i++) {
            if (Main.rand.NextBool(1000) && Vector2.Distance(Main.player[i].Center, NPC.Center) <= 16 * 10) {
                _idleTimer = 0;
                _idleMax = Main.rand.Next(60 * 2, 60 * 8);
                _currentState = State.Offering;
                _playerWhoAmI = Main.player[i].whoAmI;
            }
        }

        NPC.velocity.X = 0;
    }
    public void Walking() {
        _walkingTimer++;
        if (_walkingTimer >= _walkingMax) {
            _currentState = State.Idle;
            _walkingTimer = 0;
            _walkingMax = Main.rand.Next(60 * 2, 60 * 8);
        }

        if (NPC.collideX) {
            _walkingDir *= -1;
            NPC.spriteDirection *= -1;
        }

        NPC.velocity.X = 1 * _walkingDir;
    }
    
    public void Jumping(bool idleOnFall) {
        if (idleOnFall) {
            if (NPC.collideY) {
                _jumpClimbVel = 0;
                idleOnFall = false;
                _currentState = State.Idle;
            }
        }
        else {
            _jumpCooldown++;
            _jumpStateTimer++;

            if (_jumpCooldown >= _jumpCooldownMax && NPC.collideY) {
                NPC.velocity.Y = -8;
                _jumpCooldown = 0;
            }

            if (_jumpStateTimer >= _jumpStateMax) {
                _currentState = State.Idle;
            }
        }

        _jumpClimbVel = MathHelper.Lerp(_jumpClimbVel, 0, 0.05f);
        NPC.velocity.X = _jumpClimbVel;

        if (NPC.velocity.X > 0) {
            NPC.spriteDirection = -1; }
        else if (NPC.velocity.X < 0) {
            NPC.spriteDirection = 1;
        }
    }
    
    public void Climbing() {
        if (Main.tile[(int)NPC.position.X / 16, (int)NPC.position.Y / 16 - 1].TileType == TileID.Trees) {
            NPC.velocity.Y = -1.5f;
        }
        else {
            _climbingTimer++;
            if (_climbingTimer >= _climbingMax) {
                _climbingTimer = 0;
                _climbingMax = Main.rand.Next(60 * 5, 60 * 10);
                NPC.velocity.Y = -8;

                _jumpClimbVel = 3 * Main.rand.NextSign();
                _idleOnFall = true;
                _currentState = State.Jumping;
            }
            else {
                NPC.velocity.Y = 0;
            }
        }
        NPC.velocity.X = 0;
    }
    
    public void Offering() {
        NPC.velocity.X = 0;
        if (!Main.player[_playerWhoAmI].active || Vector2.Distance(NPC.Center, Main.player[_playerWhoAmI].Center) > 16 * 10) {
            for (int i = 0; i < Main.player.Length; i++) {
                if (Vector2.Distance(NPC.Center, Main.player[i].Center) <= 16 * 10) {
                    _playerWhoAmI = Main.player[i].whoAmI;
                    break;
                }
            }

            _playerWhoAmI = -1;
        }

        if (_playerWhoAmI == -1) {
            _currentState = State.Idle;
        }
        else {
            _offeringTimer++;
            if (_offeringTimer >= _offeringMax) {
                _offeringTimer = 0;
                _offeringMax = Main.rand.Next(60 * 5, 60 * 10);
                _currentState = State.Idle;
            }

            if (Main.player[_playerWhoAmI].Center.X > NPC.Center.X) {
                NPC.spriteDirection = -1;
            }
            else {
                NPC.spriteDirection = 1;
            }

            if (Vector2.Distance(Main.MouseWorld, NPC.Center) <= 16 * 3) {
                if (Main.mouseRight) {
                    Item.NewItem(Item.GetSource_None(), Main.LocalPlayer.Center, ItemID.Banana);
                    _currentState = State.Idle;
                }
                Main.LocalPlayer.cursorItemIconEnabled = true;
                Main.LocalPlayer.cursorItemIconID = ItemID.Banana;
            }
        }
    }
    
    public bool TreeCheck() {
        int[] treeList = { TileID.Trees, TileID.VanityTreeSakura, TileID.VanityTreeYellowWillow };
        if (treeList.Contains(Framing.GetTileSafely((int)NPC.position.X / 16, (int)NPC.position.Y / 16).TileType)
            && treeList.Contains(Framing.GetTileSafely((int)NPC.position.X / 16, (int)NPC.position.Y / 16 - 1).TileType)
            && treeList.Contains(Framing.GetTileSafely((int)NPC.position.X / 16, (int)NPC.position.Y / 16 - 2).TileType)) {
            return true;
        }

        return false;
    }
}