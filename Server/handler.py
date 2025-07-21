import json
from protocol import make_response, ERROR_CODES
from room import room_manager
from player import player_manager
from server_status import log_server_event
import threading
import time

player_count = 0
player_lock = threading.Lock()

# 클라이언트 요청 처리

def handle_client(conn, addr):
    global player_count
    deviceId = None
    nickname = None
    disconnect_reason = "정상 종료"
    try:
        while True:
            raw = conn.recv(4096)
            if not raw:
                disconnect_reason = "클라이언트가 연결을 닫음 (recv=0)"
                break
            try:
                msg = json.loads(raw.decode())
            except Exception:
                conn.sendall(make_response(False, error={"code": ERROR_CODES["JSON_PARSE"], "message": "JSON 파싱 오류"}).encode())
                continue

            msg_type = msg.get("type")
            deviceId = msg.get("deviceId")
            nickname = msg.get("nickname", "Unknown")
            sessionToken = msg.get("sessionToken")

            # 인증 및 중복 로그인 방지
            if msg_type == "auth":
                player = player_manager.add_player(conn, addr, deviceId, nickname)
                if not player:
                    conn.sendall(make_response(False, error={"code": ERROR_CODES["DUPLICATE"], "message": "중복 로그인"}).encode())
                else:
                    conn.sendall(make_response(True, data={"sessionToken": player.sessionToken}).encode())
                continue

            # 세션 체크 (실제 서비스 시 필수)
            player = player_manager.get_player(deviceId)
            if not player or player.sessionToken != sessionToken:
                conn.sendall(make_response(False, error={"code": ERROR_CODES["UNAUTHORIZED"], "message": "인증 실패"}).encode())
                continue

            if msg_type == "create":
                room, err = room_manager.create_room(msg)
                if room:
                    room_manager.room_players[room.roomId].add(deviceId)
                    room.currentPlayers = 1
                    data = {
                        "roomId": room.roomId,
                        "sessionToken": player.sessionToken,
                        "data": room.to_dict()
                    }
                    log_server_event(f"[SOCKET {addr}] [방 생성] roomId={room.roomId}, host={room.hostAddress}:{room.hostPort}, name={room.roomName}, by={deviceId}")
                    conn.sendall(make_response(True, data=data).encode())
                else:
                    conn.sendall(make_response(False, error={"code": ERROR_CODES["ROOM_FULL"], "message": err}).encode())

            elif msg_type == "list":
                include_private = msg.get("includePrivate", False)
                rooms = room_manager.get_room_list(include_private)
                log_server_event(f"방 목록 요청: by={deviceId}, 반환 방 수={len(rooms)}")
                conn.sendall(make_response(True, data={"rooms": rooms}).encode())

            elif msg_type == "join":
                roomId = msg.get("roomId")
                room, err = room_manager.join_room(roomId, deviceId)
                if room:
                    data = {
                        "hostAddress": room.hostAddress,
                        "hostPort": room.hostPort,
                        "roomInfo": room.to_dict()
                    }
                    log_server_event(f"[SOCKET {addr}] [방 참가] roomId={roomId}, by={deviceId}")
                    conn.sendall(make_response(True, data=data).encode())
                else:
                    conn.sendall(make_response(False, error={"code": ERROR_CODES["ROOM_FULL"], "message": err}).encode())

            elif msg_type == "leave":
                roomId = msg.get("roomId")
                ok, err = room_manager.leave_room(roomId, deviceId)
                if ok:
                    log_server_event(f"방 퇴장: roomId={roomId}, by={deviceId}")
                    conn.sendall(make_response(True, data={"roomId": roomId}).encode())
                else:
                    conn.sendall(make_response(False, error={"code": ERROR_CODES["NOT_FOUND"], "message": err}).encode())

            elif msg_type == "heartbeat":
                roomId = msg.get("roomId")
                if roomId in room_manager.rooms:
                    room_manager.rooms[roomId].lastHeartbeat = int(time.time())
                    log_server_event(f"하트비트: roomId={roomId}, by={deviceId}")
                    conn.sendall(make_response(True, data={"roomId": roomId}).encode())
                else:
                    conn.sendall(make_response(False, error={"code": ERROR_CODES["NOT_FOUND"], "message": "방이 존재하지 않습니다"}).encode())

            # 플레이어 목록 요청: 'playerList' 또는 'getPlayerList' 모두 지원
            elif msg_type == "playerList" or msg_type == "getPlayerList":
                roomId = msg.get("roomId")
                player_list = room_manager.get_player_list(roomId, player_manager)
                conn.sendall(make_response(True, data=player_list).encode())

            # 방 삭제 요청: 방장만 가능, sessionToken 검증
            elif msg_type == "delete":
                roomId = msg.get("roomId")
                token = msg.get("sessionToken")
                room = room_manager.rooms.get(roomId)
                if not room:
                    conn.sendall(make_response(False, error={"code": ERROR_CODES["NOT_FOUND"], "message": "방이 존재하지 않습니다"}).encode())
                    continue
                if room.hostDeviceId != deviceId:
                    conn.sendall(make_response(False, error={"code": ERROR_CODES["FORBIDDEN"], "message": "방장만 삭제할 수 있습니다"}).encode())
                    continue
                host_player = player_manager.get_player(deviceId)
                if not host_player or host_player.sessionToken != token:
                    conn.sendall(make_response(False, error={"code": ERROR_CODES["UNAUTHORIZED"], "message": "세션 토큰 불일치"}).encode())
                    continue
                # 방 삭제
                del room_manager.rooms[roomId]
                del room_manager.room_players[roomId]
                log_server_event(f"[SOCKET {addr}] [방 삭제] roomId={roomId}, by={deviceId}")
                conn.sendall(make_response(True, data={"roomId": roomId}).encode())

            else:
                conn.sendall(make_response(False, error={"code": ERROR_CODES["NOT_FOUND"], "message": "알 수 없는 요청 타입"}).encode())
    except Exception as e:
        disconnect_reason = f"서버 예외 발생: {str(e)}"
        print(f"Error: {e}")
        try:
            # 예외 발생 시에도 서버 로그에 기록
            log_server_event(f"[ERROR] {str(e)}")
            # 예외 발생 시에도 클라이언트에 에러 응답 전송
            conn.sendall(make_response(False, error={"code": 500, "message": f"서버 내부 오류: {str(e)}"}).encode())
        except Exception as send_ex:
            print(f"Error sending error response: {send_ex}")
            log_server_event(f"[ERROR] Error sending error response: {str(send_ex)}")
    finally:
        # 연결 종료 시 해당 deviceId가 속한 모든 방에서 leave_room 호출
        if deviceId:
            rooms_to_leave = []
            for roomId, players in list(room_manager.room_players.items()):
                if deviceId in players:
                    rooms_to_leave.append(roomId)
            for roomId in rooms_to_leave:
                room_manager.leave_room(roomId, deviceId)
        log_server_event(f"[연결 종료] addr={addr}, deviceId={deviceId}, reason={disconnect_reason}")
        conn.close()
        with player_lock:
            if player_count > 0:
                player_count -= 1
        player_manager.remove_player(deviceId) 